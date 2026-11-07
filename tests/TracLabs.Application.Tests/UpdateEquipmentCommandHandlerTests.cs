using FluentAssertions;
using NSubstitute;
using TracLabs.Application.Common.Exceptions;
using TracLabs.Application.Features.Equipment.Commands.UpdateEquipment;
using TracLabs.Application.Features.StandardEquipment.Commands.UpdateStandardEquipment;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using Xunit;

namespace TracLabs.Application.Tests;

public class UpdateEquipmentCommandHandlerTests
{
    private readonly IEquipmentRepository _equipmentRepo = Substitute.For<IEquipmentRepository>();
    private readonly IStandardEquipmentRepository _standardRepo = Substitute.For<IStandardEquipmentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task UpdateEquipment_WithValidCommand_ShouldUpdateAndReturnDetailDto()
    {
        // Arrange
        var equipmentId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var equipment = new Equipment(
            labTag: "VOL-001",
            model: "Mod-A",
            manufacturer: "Maker",
            serialNumber: "SN-1",
            instrumentType: "Balão Volumétrico",
            rangeMin: null,
            rangeMax: null,
            unit: "mL",
            resolution: null,
            clientId: clientId,
            nominalCapacity: 100m);

        _equipmentRepo.GetByIdAsync(equipment.Id, Arg.Any<CancellationToken>()).Returns(equipment);
        _equipmentRepo.GetByLabTagAsync("VOL-001-NEW", Arg.Any<CancellationToken>()).Returns((Equipment?)null);

        var handler = new UpdateEquipmentCommandHandler(_equipmentRepo, _unitOfWork);
        var command = new UpdateEquipmentCommand(
            Id: equipment.Id,
            LabTag: "VOL-001-NEW",
            Model: "Mod-A-Plus",
            Manufacturer: "Maker New",
            SerialNumber: "SN-1-REV",
            InstrumentType: "Balão Volumétrico",
            RangeMin: null,
            RangeMax: null,
            Unit: "mL",
            Resolution: null,
            NominalCapacity: 100m,
            Name: "Balão Volumétrico 100mL",
            CurrentLocation: "Bancada 01");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.LabTag.Should().Be("VOL-001-NEW");
        result.Model.Should().Be("Mod-A-Plus");
        result.NominalCapacity.Should().Be(100m);
        result.Resolution.Should().BeNull();
        _equipmentRepo.Received(1).Update(equipment);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateEquipment_WhenTagAlreadyExistsOnAnotherEquipment_ShouldThrowValidationException()
    {
        // Arrange
        var equipment = new Equipment("VOL-001", "Mod-A", "Maker", "SN-1", "Balão Volumétrico", null, null, "mL", null, Guid.NewGuid(), nominalCapacity: 100m);
        var anotherEquipment = new Equipment("VOL-DUPLICATE", "Mod-B", "Maker", "SN-2", "Balão Volumétrico", null, null, "mL", null, Guid.NewGuid(), nominalCapacity: 250m);

        _equipmentRepo.GetByIdAsync(equipment.Id, Arg.Any<CancellationToken>()).Returns(equipment);
        _equipmentRepo.GetByLabTagAsync("VOL-DUPLICATE", Arg.Any<CancellationToken>()).Returns(anotherEquipment);

        var handler = new UpdateEquipmentCommandHandler(_equipmentRepo, _unitOfWork);
        var command = new UpdateEquipmentCommand(
            Id: equipment.Id,
            LabTag: "VOL-DUPLICATE",
            Model: "Mod-A",
            Manufacturer: "Maker",
            SerialNumber: "SN-1",
            InstrumentType: "Balão Volumétrico",
            RangeMin: null,
            RangeMax: null,
            Unit: "mL",
            Resolution: null,
            NominalCapacity: 100m);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Já existe outro equipamento cadastrado com o Tag*");
    }

    [Fact]
    public async Task UpdateStandardEquipment_WithValidCommand_ShouldUpdateAndReturnDto()
    {
        // Arrange
        var standard = new StandardEquipment(
            labTag: "PAD-BAL-01",
            model: "XP205",
            manufacturer: "Mettler",
            serialNumber: "SN-XP205",
            instrumentType: "Balança Analítica",
            error: 0.0001m,
            expandedUncertainty: 0.0002m,
            coverageFactor: 2.0m,
            lastCalibrationDate: DateTime.UtcNow.AddMonths(-3),
            calibrationDueDate: DateTime.UtcNow.AddMonths(9),
            certificateNumber: "CERT-BAL-01",
            calibratingLaboratory: "Lab RBC",
            resolution: 0.00001m);

        _standardRepo.GetWithCertificatesAsync(standard.Id, Arg.Any<CancellationToken>()).Returns(standard);
        _standardRepo.GetByLabTagAsync("PAD-BAL-01-NEW", Arg.Any<CancellationToken>()).Returns((StandardEquipment?)null);

        var handler = new UpdateStandardEquipmentCommandHandler(_standardRepo, _unitOfWork);
        var command = new UpdateStandardEquipmentCommand(
            Id: standard.Id,
            LabTag: "PAD-BAL-01-NEW",
            Model: "XP205-Plus",
            Manufacturer: "Mettler Toledo",
            SerialNumber: "SN-XP205-REV",
            InstrumentType: "Balança Analítica",
            Resolution: 0.00001m,
            RangeMin: 0m,
            RangeMax: 220m,
            CalibrationIntervalMonths: 24,
            Name: "Balança XP205 Atualizada");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.LabTag.Should().Be("PAD-BAL-01-NEW");
        result.Model.Should().Be("XP205-Plus");
        result.CalibrationIntervalMonths.Should().Be(24);
        _standardRepo.Received(1).Update(standard);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
