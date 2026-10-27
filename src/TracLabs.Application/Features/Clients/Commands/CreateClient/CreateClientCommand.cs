using FluentValidation;
using MediatR;
using TracLabs.Application.Features.Clients.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using ValidationException = TracLabs.Application.Common.Exceptions.ValidationException;

namespace TracLabs.Application.Features.Clients.Commands.CreateClient;

public record CreateClientCommand(
    string Name,
    string TaxId,
    TaxIdType TaxIdType,
    string Email,
    string Phone,
    string ContactPerson = "",
    string Street = "",
    string City = "",
    string State = "",
    string PostalCode = "") : IRequest<ClientDto>;

public class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("O nome/razão social do cliente é obrigatório.")
            .MaximumLength(150).WithMessage("O nome não pode exceder 150 caracteres.");

        RuleFor(c => c.TaxId)
            .NotEmpty().WithMessage("O CPF/CNPJ é obrigatório.")
            .MaximumLength(20).WithMessage("O CPF/CNPJ não pode exceder 20 caracteres.");

        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail em formato inválido.")
            .MaximumLength(100).WithMessage("O e-mail não pode exceder 100 caracteres.");

        RuleFor(c => c.Phone)
            .NotEmpty().WithMessage("O telefone é obrigatório.")
            .MaximumLength(30).WithMessage("O telefone não pode exceder 30 caracteres.");
    }
}

public class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, ClientDto>
{
    private readonly IClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateClientCommandHandler(IClientRepository clientRepository, IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ClientDto> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        // Verifica unicidade de CPF/CNPJ
        var existing = await _clientRepository.GetByTaxIdAsync(request.TaxId, cancellationToken);
        if (existing != null)
        {
            throw new ValidationException(nameof(request.TaxId),
                $"Já existe um cliente cadastrado com o CPF/CNPJ '{request.TaxId}'.");
        }

        var client = new Client(
            request.Name,
            request.TaxId,
            request.TaxIdType,
            request.Email,
            request.Phone,
            request.ContactPerson,
            request.Street,
            request.City,
            request.State,
            request.PostalCode);

        await _clientRepository.AddAsync(client, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ClientDto(
            client.Id,
            client.Name,
            client.TaxId,
            client.TaxIdType,
            client.Email,
            client.Phone,
            client.ContactPerson,
            client.Street,
            client.City,
            client.State,
            client.PostalCode,
            !client.IsDeleted,
            client.CreatedAt);
    }
}
