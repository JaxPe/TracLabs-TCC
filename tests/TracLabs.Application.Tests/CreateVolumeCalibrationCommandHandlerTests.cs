using FluentAssertions;
using NSubstitute;
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
    private readonly IVolumeCalibrationService _volumeCalibrationService = Substitute.For<IVolumeCalibrationService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateVolumeCalibrationCommandHandler _handler;

    public CreateVolumeCalibrationCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("tech-001");
        _currentUserService.FullName.Returns("Ana Paula Metrologista");

        _handler = new CreateVolumeCalibrationCommandHandler(
            _equipmentRepo,
            _standardRepo,
            _calibrationRepo,
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

    private StandardEquipment CreateStandard(StandardEquipmentStatus status = StandardEquipmentStatus.Calibrated, bool isExpired = false)
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

        _volumeCalibrationService.Calculate(calcRequest).Returns(calcResult);

        var command = new CreateVolumeCalibrationCommand(
            EquipmentId: equipment.Id,
            StandardEquipmentId: standard.Id,
            NextCalibrationDue: DateTime.UtcNow.AddYears(1),
            TemperatureCelsius: 20.2m,
            HumidityPercent: 60m,
            Observations: "Calibração realizada conforme DOC-CGCRE-027.",
            CalculationRequest: calcRequest);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MeanError.Should().Be(-0.02m);
        result.ExpandedUncertainty.Should().Be(0.010m);
        result.TechnicianName.Should().Be("Ana Paula Metrologista");
        equipment.Status.Should().Be(EquipmentStatus.Calibrated);

        await _calibrationRepo.Received(1).AddAsync(Arg.Any<CalibrationRecord>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
            StandardEquipmentId: standard.Id,
            NextCalibrationDue: DateTime.UtcNow.AddYears(1),
            TemperatureCelsius: 20.0m,
            HumidityPercent: 55m,
            Observations: null,
            CalculationRequest: new VolumeCalibrationRequest { NominalVolumeMl = 100m });

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
            StandardEquipmentId: standard.Id,
            NextCalibrationDue: DateTime.UtcNow.AddYears(1),
            TemperatureCelsius: 20.0m,
            HumidityPercent: 55m,
            Observations: null,
            CalculationRequest: new VolumeCalibrationRequest { NominalVolumeMl = 100m });

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Não é possível calibrar o equipamento*");
    }
}
