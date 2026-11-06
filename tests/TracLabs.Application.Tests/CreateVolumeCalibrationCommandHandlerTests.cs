using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TracLabs.Application.DTOs.Calibration;
using TracLabs.Application.Features.Calibrations.Commands.CreateVolumeCalibration;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;
using Xunit;

namespace TracLabs.Application.Tests;

public class CreateVolumeCalibrationCommandHandlerTests
{
    private readonly IEquipmentRepository _equipmentRepo = Substitute.For<IEquipmentRepository>();
    private readonly IStandardEquipmentRepository _standardRepo = Substitute.For<IStandardEquipmentRepository>();
    private readonly ICalibrationRepository _calibrationRepo = Substitute.For<ICalibrationRepository>();
    private readonly ICertificateRepository _certificateRepo = Substitute.For<ICertificateRepository>();
    private readonly IVolumeCalibrationService _volumeCalibrationService = Substitute.For<IVolumeCalibrationService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateVolumeCalibrationCommandHandler _handler;

    private readonly StandardEquipment _liquidThermometer;
    private readonly StandardEquipment _barometer;
    private readonly StandardEquipment _thermoHygrometer;

    public CreateVolumeCalibrationCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("tech-001");
        _currentUserService.FullName.Returns("Ana Paula Metrologista");
        _certificateRepo.GenerateNextCertificateNumberAsync(Arg.Any<CancellationToken>()).Returns("CERT-2026-00001");

        _liquidThermometer = CreateStandard(StandardEquipmentStatus.Calibrated, tag: "PAD-TER-LIQ", type: "Termômetro");
        _barometer = CreateStandard(StandardEquipmentStatus.Calibrated, tag: "PAD-BAR-01", type: "Barômetro");
        _thermoHygrometer = CreateStandard(StandardEquipmentStatus.Calibrated, tag: "PAD-TH-01", type: "Termo-higrômetro");

        _standardRepo.GetByIdAsync(_liquidThermometer.Id, Arg.Any<CancellationToken>()).Returns(_liquidThermometer);
        _standardRepo.GetByIdAsync(_barometer.Id, Arg.Any<CancellationToken>()).Returns(_barometer);
        _standardRepo.GetByIdAsync(_thermoHygrometer.Id, Arg.Any<CancellationToken>()).Returns(_thermoHygrometer);
        _standardRepo.GetWithCertificatesAsync(_liquidThermometer.Id, Arg.Any<CancellationToken>()).Returns(_liquidThermometer);
        _standardRepo.GetWithCertificatesAsync(_barometer.Id, Arg.Any<CancellationToken>()).Returns(_barometer);
        _standardRepo.GetWithCertificatesAsync(_thermoHygrometer.Id, Arg.Any<CancellationToken>()).Returns(_thermoHygrometer);

        _handler = new CreateVolumeCalibrationCommandHandler(
            _equipmentRepo,
            new TracLabs.Application.Features.Calibrations.Services.VolumeStandardResolverService(_standardRepo),
            _calibrationRepo,
            _certificateRepo,
            _volumeCalibrationService,
            _currentUserService,
            _unitOfWork);
    }

    private Equipment CreateClientEquipment(EquipmentStatus status = EquipmentStatus.Received)
    {
        return new Equipment(
            labTag: "VOL-CLI-100",
            model: "Classe A",
            manufacturer: "Brand",
            serialNumber: "SN-100",
            instrumentType: "Balão Volumétrico",
            rangeMin: 0m,
            rangeMax: 100m,
            unit: "mL",
            resolution: 0.1m,
            clientId: Guid.NewGuid(),
            status: status);
    }

    private StandardEquipment CreateStandard(StandardEquipmentStatus status = StandardEquipmentStatus.Calibrated, bool isExpired = false, string tag = "PAD-BAL-01", string type = "Balança Analítica")
    {
        var calDate = DateTime.UtcNow.AddMonths(-3);
        var dueDate = isExpired ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddMonths(9);

        return new StandardEquipment(
            labTag: tag,
            model: "XP205",
            manufacturer: "Mettler Toledo",
            serialNumber: $"SN-{tag}",
            instrumentType: type,
            error: 0.0001m,
            expandedUncertainty: 0.0002m,
            coverageFactor: 2.0m,
            lastCalibrationDate: calDate,
            calibrationDueDate: dueDate,
            certificateNumber: $"CAL-{tag}-2026",
            calibratingLaboratory: "Lab RBC",
            resolution: 0.00001m,
            status: status);
    }

    private StandardEquipment CreateStandardOld(StandardEquipmentStatus status = StandardEquipmentStatus.Calibrated, bool isExpired = false)
    {
        var calDate = DateTime.UtcNow.AddMonths(-3);
        var dueDate = isExpired ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddMonths(9);

        return new StandardEquipment(
            labTag: "PAD-BAL-01",
            model: "XP205",
            manufacturer: "Mettler Toledo",
            serialNumber: "SN-XP205",
            instrumentType: "Balança Analítica",
            error: 0.0001m,
            expandedUncertainty: 0.0002m,
            coverageFactor: 2.0m,
            lastCalibrationDate: calDate,
            calibrationDueDate: dueDate,
            certificateNumber: "CAL-BAL-2026",
            calibratingLaboratory: "Lab RBC",
            resolution: 0.00001m,
            status: status);
    }

    [Fact]
    public async Task Handle_WithValidEquipmentAndUsableStandard_ShouldSucceedAndPersist()
    {
        // Arrange
        var equipment = CreateClientEquipment(EquipmentStatus.Received);
        var standard = CreateStandard(StandardEquipmentStatus.Calibrated);

        _equipmentRepo.GetByIdAsync(equipment.Id, Arg.Any<CancellationToken>())
            .Returns(equipment);
        _standardRepo.GetByIdAsync(standard.Id, Arg.Any<CancellationToken>())
            .Returns(standard);

        var calcRequest = new VolumeCalibrationRequest
        {
            NominalVolumeMl = 100m,
            Measurements = new List<VolumeMeasurementInputDto>
            {
                new() { RunNumber = 1, FullMassGrams = 200m, EmptyMassGrams = 100m, WaterTemperatureCelsius = 20m }
            }
        };

        var calcResult = new VolumeCalibrationResultDto
        {
            NominalVolumeMl = 100m,
            PointsCount = 1,
            MeanVolumeV20Ml = 99.98m,
            SystematicErrorMl = -0.02m,
            CombinedStandardUncertaintyMl = 0.005m,
            ExpandedUncertaintyMl = 0.010m,
            CoverageFactor = 2.0m,
            CalculatedPoints = new List<VolumeGravimetricPoint>
            {
                new(1, 200m, 100m, 20m, 99.98m)
            }
        };

        _volumeCalibrationService.Calculate(Arg.Any<VolumeCalibrationRequest>()).Returns(calcResult);

        var command = new CreateVolumeCalibrationCommand(
            EquipmentId: equipment.Id,
            BalanceStandardId: standard.Id,
            LiquidThermometerStandardId: _liquidThermometer.Id,
            BarometerStandardId: _barometer.Id,
            ThermoHygrometerStandardId: _thermoHygrometer.Id,
            NextCalibrationDue: DateTime.UtcNow.AddYears(1),
            TemperatureCelsius: 20.2m,
            HumidityPercent: 60m,
            Observations: "Calibração realizada conforme DOC-CGCRE-027.",
            Measurements: Enumerable.Range(1, 7).Select(i => new VolumeMeasurementInputDto { RunNumber = i, FullMassGrams = 200m, EmptyMassGrams = 100m, WaterTemperatureCelsius = 20m }).ToList());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MeanError.Should().Be(-0.02m);
        result.ExpandedUncertainty.Should().Be(0.010m);
        result.TechnicianName.Should().Be("Ana Paula Metrologista");
        equipment.Status.Should().Be(EquipmentStatus.Calibrated);

        await _calibrationRepo.Received(1).AddAsync(Arg.Any<CalibrationRecord>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStandardIsOutOfService_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var equipment = CreateClientEquipment(EquipmentStatus.Received);
        var standard = CreateStandard(StandardEquipmentStatus.OutOfService);

        _equipmentRepo.GetByIdAsync(equipment.Id, Arg.Any<CancellationToken>()).Returns(equipment);
        _standardRepo.GetByIdAsync(standard.Id, Arg.Any<CancellationToken>()).Returns(standard);

        var command = new CreateVolumeCalibrationCommand(
            EquipmentId: equipment.Id,
            BalanceStandardId: standard.Id,
            LiquidThermometerStandardId: _liquidThermometer.Id,
            BarometerStandardId: _barometer.Id,
            ThermoHygrometerStandardId: _thermoHygrometer.Id,
            NextCalibrationDue: DateTime.UtcNow.AddYears(1),
            TemperatureCelsius: 20.0m,
            HumidityPercent: 55m,
            Observations: null,
            Measurements: Enumerable.Range(1, 7).Select(i => new VolumeMeasurementInputDto { RunNumber = i, FullMassGrams = 200m, EmptyMassGrams = 100m, WaterTemperatureCelsius = 20m }).ToList());

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*não está apto para uso metrológico*");
    }

    [Fact]
    public async Task Handle_WhenEquipmentAlreadyCalibrated_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var equipment = CreateClientEquipment(EquipmentStatus.Calibrated);
        var standard = CreateStandard(StandardEquipmentStatus.Calibrated);

        _equipmentRepo.GetByIdAsync(equipment.Id, Arg.Any<CancellationToken>()).Returns(equipment);
        _standardRepo.GetByIdAsync(standard.Id, Arg.Any<CancellationToken>()).Returns(standard);

        var command = new CreateVolumeCalibrationCommand(
            EquipmentId: equipment.Id,
            BalanceStandardId: standard.Id,
            LiquidThermometerStandardId: _liquidThermometer.Id,
            BarometerStandardId: _barometer.Id,
            ThermoHygrometerStandardId: _thermoHygrometer.Id,
            NextCalibrationDue: DateTime.UtcNow.AddYears(1),
            TemperatureCelsius: 20.0m,
            HumidityPercent: 55m,
            Observations: null,
            Measurements: Enumerable.Range(1, 7).Select(i => new VolumeMeasurementInputDto { RunNumber = i, FullMassGrams = 200m, EmptyMassGrams = 100m, WaterTemperatureCelsius = 20m }).ToList());

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Não é possível calibrar o equipamento*");
    }

    [Fact]
    public async Task Handle_WithStandardRegressionCertificate_ShouldResolveDynamicCorrectionAndRecordInSnapshot()
    {
        // Arrange
        var equipment = CreateClientEquipment(EquipmentStatus.Received);
        var balance = CreateStandard(StandardEquipmentStatus.Calibrated);

        // Adiciona certificado com regressão linear ao padrão
        var points = new List<StandardCalibrationPoint>
        {
            new(1, 0.0m, 0m, 0.05m, 0.02m, 2.0m),
            new(2, 50.0m, 0m, 0.15m, 0.02m, 2.0m),
            new(3, 100.0m, 0m, 0.25m, 0.02m, 2.0m)
        };
        var cert = new StandardCertificate(
            standardEquipmentId: balance.Id,
            certificateNumber: "CERT-BAL-2026",
            calibratingLaboratory: "RBC Lab",
            calibrationDate: DateTime.UtcNow.AddMonths(-1),
            calibrationDueDate: DateTime.UtcNow.AddMonths(11),
            points: points,
            fitType: CurveFitType.LinearRegression);

        balance.AddCertificate(cert);

        _equipmentRepo.GetByIdAsync(equipment.Id, Arg.Any<CancellationToken>()).Returns(equipment);
        _standardRepo.GetWithCertificatesAsync(balance.Id, Arg.Any<CancellationToken>()).Returns(balance);

        var measurements = Enumerable.Range(1, 7).Select(i => new VolumeMeasurementInputDto
        {
            RunNumber = i,
            EmptyMassGrams = 100.0m,
            FullMassGrams = 200.0m, // Operating mass = 100g
            WaterTemperatureCelsius = 20.0m,
            AmbientTemperatureCelsius = 20.0m,
            AtmosphericPressureHpa = 1013.25m,
            RelativeHumidityPercent = 50.0m
        }).ToList();

        var calcResult = new VolumeCalibrationResultDto
        {
            NominalVolumeMl = 100m,
            PointsCount = 7,
            MeanVolumeV20Ml = 100.02m,
            SystematicErrorMl = 0.02m,
            CombinedStandardUncertaintyMl = 0.005m,
            ExpandedUncertaintyMl = 0.010m,
            CoverageFactor = 2.0m,
            CalculatedPoints = Enumerable.Range(1, 7).Select(i => new VolumeGravimetricPoint(i, 200m, 100m, 20m, 100.02m)).ToList()
        };

        _volumeCalibrationService.Calculate(Arg.Any<VolumeCalibrationRequest>()).Returns(calcResult);

        var command = new CreateVolumeCalibrationCommand(
            EquipmentId: equipment.Id,
            BalanceStandardId: balance.Id,
            LiquidThermometerStandardId: _liquidThermometer.Id,
            BarometerStandardId: _barometer.Id,
            ThermoHygrometerStandardId: _thermoHygrometer.Id,
            NextCalibrationDue: DateTime.UtcNow.AddYears(1),
            NominalVolumeMl: 100m,
            Measurements: measurements);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.StandardsUsed.Should().NotBeNull();
        result.StandardsUsed!.Should().HaveCount(4);
        var balanceUsed = result.StandardsUsed.First(s => s.Role == StandardRole.Balance);
        balanceUsed.CertificateNumber.Should().Be("CERT-BAL-2026");
        balanceUsed.OperatingValue.Should().Be(100.0m);
        balanceUsed.Correction.Should().Be(0.25m);
        balanceUsed.IsExtrapolated.Should().BeFalse();
        balanceUsed.WarningMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenNominalVolumeExceedsCalibrationRange_ShouldIncludeWarningInDtoAndObservations()
    {
        // Arrange
        var equipment = new Equipment(
            labTag: "VOL-CLI-100",
            model: "Classe A",
            manufacturer: "Brand",
            serialNumber: "SN-100",
            instrumentType: "Balão Volumétrico",
            rangeMin: 10m,
            rangeMax: 100m,
            unit: "mL",
            resolution: 0.1m,
            clientId: Guid.NewGuid(),
            calibrationRangeMin: 20m,
            calibrationRangeMax: 80m); // Calibrado apenas de 20 a 80 mL

        var standard = CreateStandard(StandardEquipmentStatus.Calibrated);

        _equipmentRepo.GetByIdAsync(equipment.Id, Arg.Any<CancellationToken>()).Returns(equipment);
        _standardRepo.GetWithCertificatesAsync(standard.Id, Arg.Any<CancellationToken>()).Returns(standard);

        var measurements = Enumerable.Range(1, 7).Select(i => new VolumeMeasurementInputDto
        {
            RunNumber = i,
            EmptyMassGrams = 100.0m,
            FullMassGrams = 200.0m,
            WaterTemperatureCelsius = 20.0m,
            AmbientTemperatureCelsius = 20.0m,
            AtmosphericPressureHpa = 1013.25m,
            RelativeHumidityPercent = 50.0m
        }).ToList();

        var calcResult = new VolumeCalibrationResultDto
        {
            NominalVolumeMl = 100m,
            PointsCount = 7,
            MeanVolumeV20Ml = 100.02m,
            SystematicErrorMl = 0.02m,
            CombinedStandardUncertaintyMl = 0.005m,
            ExpandedUncertaintyMl = 0.010m,
            CoverageFactor = 2.0m,
            CalculatedPoints = Enumerable.Range(1, 7).Select(i => new VolumeGravimetricPoint(i, 200m, 100m, 20m, 100.02m)).ToList()
        };

        _volumeCalibrationService.Calculate(Arg.Any<VolumeCalibrationRequest>()).Returns(calcResult);

        var command = new CreateVolumeCalibrationCommand(
            EquipmentId: equipment.Id,
            BalanceStandardId: standard.Id,
            LiquidThermometerStandardId: _liquidThermometer.Id,
            BarometerStandardId: _barometer.Id,
            ThermoHygrometerStandardId: _thermoHygrometer.Id,
            NextCalibrationDue: DateTime.UtcNow.AddYears(1),
            NominalVolumeMl: 100m, // 100 mL > 80 mL (extrapolação de faixa calibrada!)
            Measurements: measurements);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Warnings.Should().NotBeNullOrEmpty();
        result.Warnings!.Should().Contain(w => w.Contains("fora da margem de calibração") && w.Contains("100"));
        result.Observations.Should().Contain("[Avisos Metrológicos]");
    }

    [Fact]
    public async Task Handle_WhenUsingLegacyStandard_ShouldUseCorrectionRatherThanError()
    {
        // Arrange
        var equipment = CreateClientEquipment(EquipmentStatus.Received);
        // Standard com Error = 0.0001m, portanto Correction = -0.0001m
        var standard = CreateStandard(StandardEquipmentStatus.Calibrated);

        _equipmentRepo.GetByIdAsync(equipment.Id, Arg.Any<CancellationToken>()).Returns(equipment);
        _standardRepo.GetWithCertificatesAsync(standard.Id, Arg.Any<CancellationToken>()).Returns(standard);

        VolumeCalibrationRequest? capturedReq = null;
        _volumeCalibrationService.Calculate(Arg.Do<VolumeCalibrationRequest>(r => capturedReq = r))
            .Returns(new VolumeCalibrationResultDto
            {
                NominalVolumeMl = 100m,
                PointsCount = 7,
                MeanVolumeV20Ml = 100.0m,
                CoverageFactor = 2.0m,
                CalculatedPoints = Enumerable.Range(1, 7).Select(i => new VolumeGravimetricPoint(i, 200m, 100m, 20m, 100.0m)).ToList()
            });

        var measurements = Enumerable.Range(1, 7).Select(i => new VolumeMeasurementInputDto
        {
            RunNumber = i,
            EmptyMassGrams = 100.0m,
            FullMassGrams = 200.0m,
            WaterTemperatureCelsius = 20.0m,
            AmbientTemperatureCelsius = 20.0m,
            AtmosphericPressureHpa = 1013.25m,
            RelativeHumidityPercent = 50.0m
        }).ToList();

        var command = new CreateVolumeCalibrationCommand(
            EquipmentId: equipment.Id,
            BalanceStandardId: standard.Id,
            LiquidThermometerStandardId: _liquidThermometer.Id,
            BarometerStandardId: _barometer.Id,
            ThermoHygrometerStandardId: _thermoHygrometer.Id,
            NextCalibrationDue: DateTime.UtcNow.AddYears(1),
            NominalVolumeMl: 100m,
            Measurements: measurements);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        capturedReq.Should().NotBeNull();
        // A correção enviada ao motor de cálculo deve ser Correction (-0.0001), não Error (+0.0001)
        capturedReq!.BalanceFullCorrectionGrams.Should().Be(-0.0001m);
        result.StandardsUsed.Should().NotBeNullOrEmpty();
        var balanceUsed = result.StandardsUsed!.First(s => s.Role == StandardRole.Balance);
        balanceUsed.Correction.Should().Be(-0.0001m);
    }

    [Fact]
    public async Task Handle_WhenExceptionOccursDuringPersistence_ShouldRollbackTransactionAndRethrow()
    {
        // Arrange
        var equipment = CreateClientEquipment(EquipmentStatus.Received);
        var standard = CreateStandard(StandardEquipmentStatus.Calibrated);

        _equipmentRepo.GetByIdAsync(equipment.Id, Arg.Any<CancellationToken>()).Returns(equipment);
        _standardRepo.GetByIdAsync(standard.Id, Arg.Any<CancellationToken>()).Returns(standard);

        var calcResult = new VolumeCalibrationResultDto
        {
            NominalVolumeMl = 100m,
            PointsCount = 1,
            MeanVolumeV20Ml = 99.98m,
            SystematicErrorMl = -0.02m,
            CombinedStandardUncertaintyMl = 0.005m,
            ExpandedUncertaintyMl = 0.010m,
            CoverageFactor = 2.0m,
            CalculatedPoints = new List<VolumeGravimetricPoint>
            {
                new(1, 200m, 100m, 20m, 99.98m)
            }
        };

        _volumeCalibrationService.Calculate(Arg.Any<VolumeCalibrationRequest>()).Returns(calcResult);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Simulated database failure"));

        var command = new CreateVolumeCalibrationCommand(
            EquipmentId: equipment.Id,
            BalanceStandardId: standard.Id,
            LiquidThermometerStandardId: _liquidThermometer.Id,
            BarometerStandardId: _barometer.Id,
            ThermoHygrometerStandardId: _thermoHygrometer.Id,
            NextCalibrationDue: DateTime.UtcNow.AddYears(1),
            TemperatureCelsius: 20.2m,
            HumidityPercent: 60m,
            Observations: "Teste rollback",
            Measurements: Enumerable.Range(1, 7).Select(i => new VolumeMeasurementInputDto { RunNumber = i, FullMassGrams = 200m, EmptyMassGrams = 100m, WaterTemperatureCelsius = 20m }).ToList());

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated database failure");

        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitTransactionAsync(Arg.Any<CancellationToken>());
    }
}
