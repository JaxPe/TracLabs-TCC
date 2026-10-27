using FluentAssertions;
using NSubstitute;
using TracLabs.Application.Common.Exceptions;
using TracLabs.Application.Features.Clients.Commands.CreateClient;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using Xunit;

namespace TracLabs.Application.Tests;

public class CreateClientCommandHandlerTests
{
    private readonly IClientRepository _clientRepo = Substitute.For<IClientRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateClientCommandHandler _handler;

    public CreateClientCommandHandlerTests()
    {
        _handler = new CreateClientCommandHandler(_clientRepo, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithUniqueTaxId_ShouldCreateAndPersistClient()
    {
        // Arrange
        var command = new CreateClientCommand(
            Name: "Petrobras S.A.",
            TaxId: "33000167000101",
            TaxIdType: TaxIdType.Cnpj,
            Email: "contato@petrobras.com.br",
            Phone: "2138760000",
            ContactPerson: "Carlos Metrologia");

        _clientRepo.GetByTaxIdAsync(command.TaxId, Arg.Any<CancellationToken>())
            .Returns((Client?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Petrobras S.A.");
        result.TaxId.Should().Be("33000167000101");
        result.IsActive.Should().BeTrue();

        await _clientRepo.Received(1).AddAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateTaxId_ShouldThrowValidationException()
    {
        // Arrange
        var command = new CreateClientCommand(
            Name: "Cliente Duplicado",
            TaxId: "33000167000101",
            TaxIdType: TaxIdType.Cnpj,
            Email: "dup@email.com",
            Phone: "2199999999");

        var existingClient = new Client(
            "Cliente Já Existente",
            command.TaxId,
            command.TaxIdType,
            "existente@email.com",
            "2199999999",
            "", "", "", "", "");

        _clientRepo.GetByTaxIdAsync(command.TaxId, Arg.Any<CancellationToken>())
            .Returns(existingClient);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Já existe um cliente cadastrado com o CPF/CNPJ*");

        await _clientRepo.DidNotReceive().AddAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>());
    }
}
