using FluentAssertions;
using NSubstitute;
using TracLabs.Application.Common.Exceptions;
using TracLabs.Application.Features.StandardEquipment.Commands.CreateStandardEquipment;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using Xunit;

namespace TracLabs.Application.Tests;

public class CreateStandardEquipmentCommandHandlerTests
{
    private readonly IStandardEquipmentRepository _standardRepo = Substitute.For<IStandardEquipmentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateStandardEquipmentCommandHandler _handler;

    public CreateStandardEquipmentCommandHandlerTests()
    {
        _handler = new CreateStandardEquipmentCommandHandler(_standardRepo, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithUniqueLabTag_ShouldCreateAndPersistStandard()
    {
        // Arrange
        var command = new CreateStandardEquipmentCommand(
            LabTag: "PAD-VOL-02",
            Model: "XP205",
            Manufacturer: "Mettler Toledo",
            SerialNumber: "SN-XP205-02",
            InstrumentType: "Balança Analítica",
            Error: 0.0001m,
            ExpandedUncertainty: 0.0002m,
            CoverageFactor: 2.0m,
            LastCalibrationDate: DateTime.UtcNow.AddMonths(-2),
            CalibrationDueDate: DateTime.UtcNow.AddMonths(10),
            CalibrationCertificateNumber: "CERT-2026-002",
            CalibratingLaboratory: "Inmetro RBC",
            Resolution: 0.00001m);

        _standardRepo.GetByLabTagAsync(command.LabTag, Arg.Any<CancellationToken>())
            .Returns((StandardEquipment?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.LabTag.Should().Be("PAD-VOL-02");
        result.IsUsableForCalibration.Should().BeTrue();

        await _standardRepo.Received(1).AddAsync(Arg.Any<StandardEquipment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateLabTag_ShouldThrowValidationException()
    {
        // Arrange
        var command = new CreateStandardEquipmentCommand(
            LabTag: "PAD-VOL-02",
            Model: "XP205",
            Manufacturer: "Mettler Toledo",
            SerialNumber: "SN-XP205-02",
            InstrumentType: "Balança Analítica",
            Error: 0.0001m,
            ExpandedUncertainty: 0.0002m,
            CoverageFactor: 2.0m,
            LastCalibrationDate: DateTime.UtcNow.AddMonths(-2),
            CalibrationDueDate: DateTime.UtcNow.AddMonths(10),
            CalibrationCertificateNumber: "CERT-2026-002",
            CalibratingLaboratory: "Inmetro RBC");

        var existing = new StandardEquipment(
            command.LabTag, command.Model, command.Manufacturer, command.SerialNumber,
            command.InstrumentType, command.Error, command.ExpandedUncertainty,
            command.CoverageFactor, command.LastCalibrationDate, command.CalibrationDueDate,
            command.CalibrationCertificateNumber, command.CalibratingLaboratory);

        _standardRepo.GetByLabTagAsync(command.LabTag, Arg.Any<CancellationToken>())
            .Returns(existing);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Já existe um padrão cadastrado com o Tag*");

        await _standardRepo.DidNotReceive().AddAsync(Arg.Any<StandardEquipment>(), Arg.Any<CancellationToken>());
    }
}
