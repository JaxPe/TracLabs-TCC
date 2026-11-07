using FluentAssertions;
using NSubstitute;
using TracLabs.Application.DTOs.Calibration;
using TracLabs.Application.Features.Calibrations.Commands.CalculateVolume;
using TracLabs.Application.Features.Calibrations.Services;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;
using TracLabs.Infrastructure.Services;
using Xunit;

namespace TracLabs.Application.Tests;

public class CalculateVolumeCalibrationCommandHandlerTests
{
    private readonly IStandardEquipmentRepository _standardRepo = Substitute.For<IStandardEquipmentRepository>();
    private readonly VolumeCalibrationService _service = new();
    private readonly VolumeStandardResolverService _resolver;
    private readonly CalculateVolumeCalibrationCommandHandler _handler;

    private readonly StandardEquipment _balance;
    private readonly StandardEquipment _liquidThermometer;
    private readonly StandardEquipment _barometer;
    private readonly StandardEquipment _ambientThermometer;
    private readonly StandardEquipment _hygrometer;
    private readonly StandardEquipment _thermoHygrometer;

    public CalculateVolumeCalibrationCommandHandlerTests()
    {
        _resolver = new VolumeStandardResolverService(_standardRepo);
        _handler = new CalculateVolumeCalibrationCommandHandler(_resolver, _service);

        _balance = CreateMockStandard("PAD-BAL-01", "Balança Analítica", 0.0001m, 0.0002m, 0.00001m);
        _liquidThermometer = CreateMockStandard("PAD-TER-LIQ", "Termômetro do Líquido", -0.02m, 0.01m, 0.01m);
        _barometer = CreateMockStandard("PAD-BAR-01", "Barômetro Digital", -1.5m, 0.3m, 0.1m);
        _ambientThermometer = CreateMockStandard("PAD-TER-AMB", "Termômetro Ambiente", 0.05m, 0.02m, 0.1m);
        _hygrometer = CreateMockStandard("PAD-HIG-01", "Higrômetro", 1.0m, 1.5m, 0.5m);
        _thermoHygrometer = CreateMockStandard("PAD-TH-01", "Termo-higrômetro", 0.02m, 0.03m, 0.1m);

        SetupStandardMock(_balance);
        SetupStandardMock(_liquidThermometer);
        SetupStandardMock(_barometer);
        SetupStandardMock(_ambientThermometer);
        SetupStandardMock(_hygrometer);
        SetupStandardMock(_thermoHygrometer);
    }

    private void SetupStandardMock(StandardEquipment standard)
    {
        _standardRepo.GetByIdAsync(standard.Id, Arg.Any<CancellationToken>()).Returns(standard);
        _standardRepo.GetWithCertificatesAsync(standard.Id, Arg.Any<CancellationToken>()).Returns(standard);
    }

    private static StandardEquipment CreateMockStandard(string tag, string type, decimal error, decimal uncertainty, decimal resolution)
    {
        var standard = new StandardEquipment(
            labTag: tag,
            model: "Model-Test",
            manufacturer: "Precision Labs",
            serialNumber: $"SN-{tag}",
            instrumentType: type,
            error: error,
            expandedUncertainty: uncertainty,
            coverageFactor: 2.0m,
            lastCalibrationDate: DateTime.UtcNow.AddMonths(-2),
            calibrationDueDate: DateTime.UtcNow.AddMonths(10),
            certificateNumber: $"CERT-{tag}-2026",
            calibratingLaboratory: "Inmetro RBC",
            resolution: resolution,
            status: StandardEquipmentStatus.Calibrated);

        return standard;
    }

    private static List<VolumeMeasurementInputDto> CreateSampleMeasurements(int count = 10)
    {
        return Enumerable.Range(1, count).Select(i => new VolumeMeasurementInputDto
        {
            RunNumber = i,
            EmptyMassGrams = 100.0m,
            FullMassGrams = 200.0m + (i * 0.001m),
            WaterTemperatureCelsius = 20.0m + (i * 0.01m),
            AmbientTemperatureCelsius = 20.2m,
            AtmosphericPressureHpa = 1013.25m,
            RelativeHumidityPercent = 52.0m
        }).ToList();
    }

    [Fact]
    public async Task Handle_WithAll5Standards_ShouldResolveCertificatesAndCalculateRealisticMetrology()
    {
        // Arrange
        var command = new CalculateVolumeCalibrationCommand(
            NominalVolumeMl: 100.0m,
            Material: VolumetricMaterial.BorosilicateGlass33,
            Measurements: CreateSampleMeasurements(10),
            BalanceStandardId: _balance.Id,
            LiquidThermometerStandardId: _liquidThermometer.Id,
            BarometerStandardId: _barometer.Id,
            AmbientThermometerStandardId: _ambientThermometer.Id,
            HygrometerStandardId: _hygrometer.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.NominalVolumeMl.Should().Be(100.0m);
        result.PointsCount.Should().Be(10);
        result.MeanVolumeV20Ml.Should().BeApproximately(100.0m, 1.0m);
        result.ExpandedUncertaintyMl.Should().BeGreaterThan(0m);
        result.CoverageFactor.Should().BeInRange(2.0m, 2.5m);

        // Orçamento do Ar deve conter contribuições dos padrões
        result.AirDensityUncertaintyBudget.Should().NotBeEmpty();
        var pCert = result.AirDensityUncertaintyBudget.FirstOrDefault(b => b.Symbol == "Pcert");
        pCert.Should().NotBeNull();
        pCert!.UncertaintyContributionMl.Should().BeGreaterThan(0m);

        // Orçamento do Volume deve conter contribuições da Balança e do Termômetro do Líquido
        result.UncertaintyBudget.Should().NotBeEmpty();
        var mCert = result.UncertaintyBudget.FirstOrDefault(b => b.Symbol == "Mccert");
        mCert.Should().NotBeNull();
        mCert!.UncertaintyContributionMl.Should().BeGreaterThan(0m);

        var tCert = result.UncertaintyBudget.FirstOrDefault(b => b.Symbol == "Tcert");
        tCert.Should().NotBeNull();
        tCert!.UncertaintyContributionMl.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task Handle_WithThermoHygrometer_ShouldSucceedWith4PhysicalStandards()
    {
        // Arrange — 4 padrões físicos: Balança, Termômetro Líquido, Barômetro e Termo-higrômetro único
        var command = new CalculateVolumeCalibrationCommand(
            NominalVolumeMl: 100.0m,
            Material: VolumetricMaterial.BorosilicateGlass33,
            Measurements: CreateSampleMeasurements(10),
            BalanceStandardId: _balance.Id,
            LiquidThermometerStandardId: _liquidThermometer.Id,
            BarometerStandardId: _barometer.Id,
            ThermoHygrometerStandardId: _thermoHygrometer.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PointsCount.Should().Be(10);
        result.ExpandedUncertaintyMl.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void Validator_WhenLiquidThermometerMissing_ShouldFailValidation()
    {
        // Arrange
        var validator = new CalculateVolumeCalibrationCommandValidator();
        var command = new CalculateVolumeCalibrationCommand(
            NominalVolumeMl: 100.0m,
            Measurements: CreateSampleMeasurements(7),
            BalanceStandardId: _balance.Id,
            LiquidThermometerStandardId: Guid.Empty, // Ausente!
            BarometerStandardId: _barometer.Id,
            ThermoHygrometerStandardId: _thermoHygrometer.Id);

        // Act
        var valResult = validator.Validate(command);

        // Assert
        valResult.IsValid.Should().BeFalse();
        valResult.Errors.Should().Contain(e => e.ErrorMessage.Contains("termômetro de imersão"));
    }

    [Fact]
    public void Validator_WhenBarometerMissing_ShouldFailValidation()
    {
        // Arrange
        var validator = new CalculateVolumeCalibrationCommandValidator();
        var command = new CalculateVolumeCalibrationCommand(
            NominalVolumeMl: 100.0m,
            Measurements: CreateSampleMeasurements(7),
            BalanceStandardId: _balance.Id,
            LiquidThermometerStandardId: _liquidThermometer.Id,
            BarometerStandardId: Guid.Empty, // Ausente!
            ThermoHygrometerStandardId: _thermoHygrometer.Id);

        // Act
        var valResult = validator.Validate(command);

        // Assert
        valResult.IsValid.Should().BeFalse();
        valResult.Errors.Should().Contain(e => e.ErrorMessage.Contains("barômetro"));
    }

    [Fact]
    public void Validator_WhenAmbientStandardsMissing_ShouldFailValidation()
    {
        // Arrange — Sem termômetro ambiente, sem higrômetro e sem termo-higrômetro
        var validator = new CalculateVolumeCalibrationCommandValidator();
        var command = new CalculateVolumeCalibrationCommand(
            NominalVolumeMl: 100.0m,
            Measurements: CreateSampleMeasurements(7),
            BalanceStandardId: _balance.Id,
            LiquidThermometerStandardId: _liquidThermometer.Id,
            BarometerStandardId: _barometer.Id);

        // Act
        var valResult = validator.Validate(command);

        // Assert
        valResult.IsValid.Should().BeFalse();
        valResult.Errors.Should().Contain(e => e.ErrorMessage.Contains("Termômetro Ambiente e o Higrômetro"));
    }

    [Fact]
    public async Task Handle_WhenStandardExpired_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var expiredThermometer = new StandardEquipment(
            labTag: "PAD-EXP-01",
            model: "Model-Exp",
            manufacturer: "Precision",
            serialNumber: "SN-EXP",
            instrumentType: "Termômetro",
            error: 0.01m,
            expandedUncertainty: 0.02m,
            coverageFactor: 2.0m,
            lastCalibrationDate: DateTime.UtcNow.AddMonths(-14),
            calibrationDueDate: DateTime.UtcNow.AddDays(-1), // Vencido!
            certificateNumber: "CERT-EXP-2025",
            calibratingLaboratory: "Inmetro",
            resolution: 0.01m,
            status: StandardEquipmentStatus.Calibrated);

        SetupStandardMock(expiredThermometer);

        var command = new CalculateVolumeCalibrationCommand(
            NominalVolumeMl: 100.0m,
            Measurements: CreateSampleMeasurements(7),
            BalanceStandardId: _balance.Id,
            LiquidThermometerStandardId: expiredThermometer.Id,
            BarometerStandardId: _barometer.Id,
            ThermoHygrometerStandardId: _thermoHygrometer.Id);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*não está apto para uso metrológico*");
    }
}
