using FluentAssertions;
using NSubstitute;
using TracLabs.Application.DTOs.Calibration;
using TracLabs.Application.Features.Calibrations.Commands.CreateVolumeCalibration;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using TracLabs.Infrastructure.Services;
using Xunit;

namespace TracLabs.Application.Tests;

public class VolumeCalibrationMetrologyTests
{
    private readonly VolumeCalibrationService _service = new();

    private readonly IEquipmentRepository _equipmentRepo = Substitute.For<IEquipmentRepository>();
    private readonly IStandardEquipmentRepository _standardRepo = Substitute.For<IStandardEquipmentRepository>();
    private readonly ICalibrationRepository _calibrationRepo = Substitute.For<ICalibrationRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private StandardEquipment CreateMockStandard(Guid id, string tag, string type, StandardRole role)
    {
        var standard = new StandardEquipment(
            labTag: tag,
            model: "Model-X",
            manufacturer: "Precision Corp",
            serialNumber: $"SN-{tag}",
            instrumentType: type,
            error: 0.001m,
            expandedUncertainty: 0.002m,
            coverageFactor: 2.0m,
            lastCalibrationDate: DateTime.UtcNow.AddMonths(-2),
            calibrationDueDate: DateTime.UtcNow.AddMonths(10),
            certificateNumber: $"CERT-{tag}",
            calibratingLaboratory: "Inmetro RBC",
            resolution: 0.0001m,
            status: StandardEquipmentStatus.Calibrated);

        typeof(BaseEntity)
            .GetProperty(nameof(BaseEntity.Id))!
            .SetValue(standard, id);

        return standard;
    }

    [Fact]
    public void Calculate_WithFewerThan7Points_ShouldThrowArgumentException()
    {
        // Arrange — 6 repetições (inválido conforme ISO 4787 e DOC-CGCRE-027)
        var request = new VolumeCalibrationRequest
        {
            NominalVolumeMl = 100m,
            Measurements = Enumerable.Range(1, 6).Select(i => new VolumeMeasurementInputDto
            {
                RunNumber = i,
                FullMassGrams = 200m + i * 0.001m,
                EmptyMassGrams = 100m,
                WaterTemperatureCelsius = 20.0m
            }).ToList()
        };

        // Act
        var act = () => _service.Calculate(request);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*no mínimo 7 pontos*");
    }

    [Fact]
    public void Calculate_WithPerPointAmbientConditions_ShouldCalculateAirDensityAnd9ComponentUncertainty()
    {
        // Arrange — 11 repetições com condições ambientais dinâmicas por ponto
        var request = new VolumeCalibrationRequest
        {
            NominalVolumeMl = 100m,
            Material = VolumetricMaterial.SodaLimeGlass,
            BalanceResolutionGrams = 0.001m,
            BalanceCertificateUncertaintyGrams = 0.001m,
            WaterThermometerResolutionCelsius = 0.01m,
            WaterThermometerUncertaintyCelsius = 0.005m,
            AmbientThermometerResolutionCelsius = 0.1m,
            AmbientThermometerUncertaintyCelsius = 0.05m,
            BarometerResolutionHpa = 0.1m,
            BarometerUncertaintyHpa = 0.5m,
            HygrometerResolutionPercent = 1.0m,
            HygrometerUncertaintyPercent = 2.0m,
            Measurements = new List<VolumeMeasurementInputDto>
            {
                new() { RunNumber = 1, FullMassGrams = 259.739m, EmptyMassGrams = 160.078m, WaterTemperatureCelsius = 20.03m, AmbientTemperatureCelsius = 20.2m, AtmosphericPressureHpa = 1018.0m, RelativeHumidityPercent = 68.0m },
                new() { RunNumber = 2, FullMassGrams = 259.738m, EmptyMassGrams = 160.079m, WaterTemperatureCelsius = 19.97m, AmbientTemperatureCelsius = 20.2m, AtmosphericPressureHpa = 1018.1m, RelativeHumidityPercent = 68.1m },
                new() { RunNumber = 3, FullMassGrams = 259.732m, EmptyMassGrams = 160.079m, WaterTemperatureCelsius = 19.96m, AmbientTemperatureCelsius = 20.3m, AtmosphericPressureHpa = 1018.0m, RelativeHumidityPercent = 68.0m },
                new() { RunNumber = 4, FullMassGrams = 259.735m, EmptyMassGrams = 160.078m, WaterTemperatureCelsius = 19.98m, AmbientTemperatureCelsius = 20.2m, AtmosphericPressureHpa = 1017.9m, RelativeHumidityPercent = 67.9m },
                new() { RunNumber = 5, FullMassGrams = 259.736m, EmptyMassGrams = 160.081m, WaterTemperatureCelsius = 20.02m, AmbientTemperatureCelsius = 20.3m, AtmosphericPressureHpa = 1018.0m, RelativeHumidityPercent = 68.0m },
                new() { RunNumber = 6, FullMassGrams = 259.738m, EmptyMassGrams = 160.082m, WaterTemperatureCelsius = 20.02m, AmbientTemperatureCelsius = 20.3m, AtmosphericPressureHpa = 1018.2m, RelativeHumidityPercent = 68.2m },
                new() { RunNumber = 7, FullMassGrams = 259.737m, EmptyMassGrams = 160.080m, WaterTemperatureCelsius = 20.01m, AmbientTemperatureCelsius = 20.2m, AtmosphericPressureHpa = 1018.0m, RelativeHumidityPercent = 68.0m },
                new() { RunNumber = 8, FullMassGrams = 259.740m, EmptyMassGrams = 160.080m, WaterTemperatureCelsius = 20.04m, AmbientTemperatureCelsius = 20.3m, AtmosphericPressureHpa = 1018.1m, RelativeHumidityPercent = 68.1m },
                new() { RunNumber = 9, FullMassGrams = 259.737m, EmptyMassGrams = 160.078m, WaterTemperatureCelsius = 20.03m, AmbientTemperatureCelsius = 20.2m, AtmosphericPressureHpa = 1018.0m, RelativeHumidityPercent = 67.9m },
                new() { RunNumber = 10, FullMassGrams = 259.742m, EmptyMassGrams = 160.077m, WaterTemperatureCelsius = 20.05m, AmbientTemperatureCelsius = 20.3m, AtmosphericPressureHpa = 1018.0m, RelativeHumidityPercent = 68.0m },
                new() { RunNumber = 11, FullMassGrams = 259.740m, EmptyMassGrams = 160.079m, WaterTemperatureCelsius = 20.03m, AmbientTemperatureCelsius = 20.2m, AtmosphericPressureHpa = 1018.1m, RelativeHumidityPercent = 68.1m }
            }
        };

        // Act
        var result = _service.Calculate(request);

        // Assert
        result.Should().NotBeNull();
        result.PointsCount.Should().Be(11);
        result.AirDensityKgPerM3.Should().BeInRange(1.18m, 1.22m);
        result.AirDensityExpandedUncertaintyKgPerM3.Should().BeGreaterThan(0m);
        result.AirDensityCoverageFactor.Should().BeGreaterThanOrEqualTo(2.0m);
        result.AirDensityUncertaintyBudget.Should().NotBeNull();
        result.AirDensityUncertaintyBudget!.Count.Should().Be(9); // Prepe, Presol, Pcert, Trepe, Tresol, Tcert, Urepe, Uresol, Ucert

        // Cada ponto individual deve registrar sua própria densidade do ar calculada e condições
        foreach (var point in result.CalculatedPoints)
        {
            point.AmbientTemperatureCelsius.Should().BeInRange(20.0m, 20.5m);
            point.AtmosphericPressureHpa.Should().BeInRange(1017.0m, 1019.0m);
            point.RelativeHumidityPercent.Should().BeInRange(67.0m, 69.0m);
            point.CalculatedAirDensityKgPerM3.Should().BeInRange(1.18m, 1.22m);
        }
    }

    [Fact]
    public void Validator_WhenMeasurementsLessThan7_ShouldFailValidation()
    {
        // Arrange
        var validator = new CreateVolumeCalibrationCommandValidator();
        var command = new CreateVolumeCalibrationCommand(
            EquipmentId: Guid.NewGuid(),
            BalanceStandardId: Guid.NewGuid(),
            Measurements: Enumerable.Range(1, 6).Select(i => new VolumeMeasurementInputDto
            {
                RunNumber = i,
                FullMassGrams = 100m + i,
                EmptyMassGrams = 50m,
                WaterTemperatureCelsius = 20m
            }).ToList());

        // Act
        var valResult = validator.Validate(command);

        // Assert
        valResult.IsValid.Should().BeFalse();
        valResult.Errors.Should().Contain(e => e.ErrorMessage.Contains("mínimo 7"));
    }

    [Fact]
    public async Task Handle_WhenMicropipetteAndFewerThan10Points_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var equipment = new Equipment(
            labTag: "PIP-CLI-01",
            model: "Pipetman P100",
            manufacturer: "Gilson",
            serialNumber: "SN-PIP-01",
            instrumentType: "Micropipeta Monocanal",
            rangeMin: 10m,
            rangeMax: 100m,
            unit: "uL",
            resolution: 0.1m,
            clientId: Guid.NewGuid(),
            status: EquipmentStatus.Received);

        var balance = CreateMockStandard(Guid.NewGuid(), "PAD-BAL", "Balança", StandardRole.Balance);

        _equipmentRepo.GetByIdAsync(equipment.Id, Arg.Any<CancellationToken>()).Returns(equipment);
        _standardRepo.GetByIdAsync(balance.Id, Arg.Any<CancellationToken>()).Returns(balance);

        var handler = new CreateVolumeCalibrationCommandHandler(
            _equipmentRepo,
            _standardRepo,
            _calibrationRepo,
            _service,
            _currentUserService,
            _unitOfWork);

        var command = new CreateVolumeCalibrationCommand(
            EquipmentId: equipment.Id,
            BalanceStandardId: balance.Id,
            Measurements: Enumerable.Range(1, 8).Select(i => new VolumeMeasurementInputDto
            {
                RunNumber = i,
                FullMassGrams = 0.100m,
                EmptyMassGrams = 0m,
                WaterTemperatureCelsius = 20.0m
            }).ToList());

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no mínimo 10 repetições*");
    }

    [Fact]
    public async Task Handle_WithAll5Standards_ShouldCreateAndPersistFrozenSnapshots()
    {
        // Arrange
        var equipment = new Equipment(
            labTag: "BAL-CLI-100",
            model: "Classe A",
            manufacturer: "Brand",
            serialNumber: "SN-100",
            instrumentType: "Balão Volumétrico",
            rangeMin: 0m,
            rangeMax: 100m,
            unit: "mL",
            resolution: 0.1m,
            clientId: Guid.NewGuid(),
            status: EquipmentStatus.Received);

        var balanceId = Guid.NewGuid();
        var liquidThermId = Guid.NewGuid();
        var ambThermId = Guid.NewGuid();
        var baromId = Guid.NewGuid();
        var hygroId = Guid.NewGuid();

        var balance = CreateMockStandard(balanceId, "PAD-BAL-01", "Balança Analítica", StandardRole.Balance);
        var liquidTherm = CreateMockStandard(liquidThermId, "PAD-TER-LIQ", "Termômetro de Imersão", StandardRole.LiquidThermometer);
        var ambTherm = CreateMockStandard(ambThermId, "PAD-TER-AMB", "Termômetro Ambiente", StandardRole.AmbientThermometer);
        var barom = CreateMockStandard(baromId, "PAD-BAR-01", "Barômetro Digital", StandardRole.Barometer);
        var hygro = CreateMockStandard(hygroId, "PAD-HIG-01", "Termo-higrômetro", StandardRole.Hygrometer);

        _equipmentRepo.GetByIdAsync(equipment.Id, Arg.Any<CancellationToken>()).Returns(equipment);
        _standardRepo.GetByIdAsync(balanceId, Arg.Any<CancellationToken>()).Returns(balance);
        _standardRepo.GetByIdAsync(liquidThermId, Arg.Any<CancellationToken>()).Returns(liquidTherm);
        _standardRepo.GetByIdAsync(ambThermId, Arg.Any<CancellationToken>()).Returns(ambTherm);
        _standardRepo.GetByIdAsync(baromId, Arg.Any<CancellationToken>()).Returns(barom);
        _standardRepo.GetByIdAsync(hygroId, Arg.Any<CancellationToken>()).Returns(hygro);

        _currentUserService.UserId.Returns("tech-42");
        _currentUserService.FullName.Returns("Carlos Metrologista");

        var handler = new CreateVolumeCalibrationCommandHandler(
            _equipmentRepo,
            _standardRepo,
            _calibrationRepo,
            _service,
            _currentUserService,
            _unitOfWork);

        var measurements = Enumerable.Range(1, 10).Select(i => new VolumeMeasurementInputDto
        {
            RunNumber = i,
            FullMassGrams = 200m + i * 0.001m,
            EmptyMassGrams = 100m,
            WaterTemperatureCelsius = 20.0m + i * 0.01m,
            AmbientTemperatureCelsius = 20.2m,
            AtmosphericPressureHpa = 1018.0m,
            RelativeHumidityPercent = 65.0m
        }).ToList();

        var command = new CreateVolumeCalibrationCommand(
            EquipmentId: equipment.Id,
            BalanceStandardId: balanceId,
            LiquidThermometerStandardId: liquidThermId,
            AmbientThermometerStandardId: ambThermId,
            BarometerStandardId: baromId,
            HygrometerStandardId: hygroId,
            NominalVolumeMl: 100m,
            Measurements: measurements);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.StandardsUsed.Should().HaveCount(5);

        var balanceUsed = result.StandardsUsed.First(s => s.Role == StandardRole.Balance);
        balanceUsed.LabTag.Should().Be("PAD-BAL-01");
        balanceUsed.CertificateNumber.Should().Be("CERT-PAD-BAL-01");
        balanceUsed.Resolution.Should().Be(0.0001m);
        balanceUsed.Correction.Should().Be(-0.001m);

        var liquidThermUsed = result.StandardsUsed.First(s => s.Role == StandardRole.LiquidThermometer);
        liquidThermUsed.LabTag.Should().Be("PAD-TER-LIQ");

        var ambThermUsed = result.StandardsUsed.First(s => s.Role == StandardRole.AmbientThermometer);
        ambThermUsed.LabTag.Should().Be("PAD-TER-AMB");

        var baromUsed = result.StandardsUsed.First(s => s.Role == StandardRole.Barometer);
        baromUsed.LabTag.Should().Be("PAD-BAR-01");

        var hygroUsed = result.StandardsUsed.First(s => s.Role == StandardRole.Hygrometer);
        hygroUsed.LabTag.Should().Be("PAD-HIG-01");

        await _calibrationRepo.Received(1).AddAsync(Arg.Is<CalibrationRecord>(r =>
            r.StandardsUsed.Count == 5 &&
            r.MeasurementPoints.Count == 10
        ), Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
