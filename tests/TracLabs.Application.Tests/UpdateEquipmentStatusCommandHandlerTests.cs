using FluentAssertions;
using NSubstitute;
using TracLabs.Application.Features.Equipment.Commands.UpdateEquipmentStatus;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using Xunit;

namespace TracLabs.Application.Tests;

public class UpdateEquipmentStatusCommandHandlerTests
{
    private readonly IEquipmentRepository _equipmentRepo = Substitute.For<IEquipmentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task UpdateEquipmentStatus_WithValidStatus_ShouldUpdateStatusAndSave()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var equipment = new Equipment(
            labTag: "BAL-CLI-001",
            model: "Blaubrand",
            manufacturer: "Brand",
            serialNumber: "SN-987654",
            instrumentType: "Balão Volumétrico",
            rangeMin: null,
            rangeMax: null,
            unit: "mL",
            resolution: null,
            clientId: clientId,
            nominalCapacity: 100m);

        equipment.Status.Should().Be(EquipmentStatus.Received);
        _equipmentRepo.GetByIdAsync(equipment.Id, Arg.Any<CancellationToken>()).Returns(equipment);

        var handler = new UpdateEquipmentStatusCommandHandler(_equipmentRepo, _unitOfWork);
        var command = new UpdateEquipmentStatusCommand(equipment.Id, EquipmentStatus.Approved);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(EquipmentStatus.Approved);
        equipment.Status.Should().Be(EquipmentStatus.Approved);
        _equipmentRepo.Received(1).Update(equipment);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateEquipmentStatus_WhenRecalibratingFromCalibratedToReceived_ShouldSucceed()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var equipment = new Equipment(
            labTag: "BAL-CLI-001",
            model: "Blaubrand",
            manufacturer: "Brand",
            serialNumber: "SN-987654",
            instrumentType: "Balão Volumétrico",
            rangeMin: null,
            rangeMax: null,
            unit: "mL",
            resolution: null,
            clientId: clientId,
            nominalCapacity: 100m);

        equipment.UpdateStatus(EquipmentStatus.Calibrated);
        equipment.Status.Should().Be(EquipmentStatus.Calibrated);

        _equipmentRepo.GetByIdAsync(equipment.Id, Arg.Any<CancellationToken>()).Returns(equipment);

        var handler = new UpdateEquipmentStatusCommandHandler(_equipmentRepo, _unitOfWork);
        var command = new UpdateEquipmentStatusCommand(equipment.Id, EquipmentStatus.Received);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(EquipmentStatus.Received);
        equipment.Status.Should().Be(EquipmentStatus.Received);
        _equipmentRepo.Received(1).Update(equipment);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateEquipmentStatus_WhenNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _equipmentRepo.GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>()).Returns((Equipment?)null);

        var handler = new UpdateEquipmentStatusCommandHandler(_equipmentRepo, _unitOfWork);
        var command = new UpdateEquipmentStatusCommand(nonExistentId, EquipmentStatus.Approved);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{nonExistentId}*");
    }
    [Theory]
    [InlineData(EquipmentStatus.Approved, null, EquipmentStatus.Approved)]
    [InlineData(null, EquipmentStatus.NonConforming, EquipmentStatus.NonConforming)]
    [InlineData(EquipmentStatus.Calibrated, EquipmentStatus.InCalibration, EquipmentStatus.InCalibration)]
    public void UpdateEquipmentStatusRequest_ShouldResolveStatusProperly(
        EquipmentStatus? status,
        EquipmentStatus? newStatus,
        EquipmentStatus expected)
    {
        var req = new UpdateEquipmentStatusRequest
        {
            Status = status,
            NewStatus = newStatus
        };

        req.Value.Should().Be(expected);
    }
}