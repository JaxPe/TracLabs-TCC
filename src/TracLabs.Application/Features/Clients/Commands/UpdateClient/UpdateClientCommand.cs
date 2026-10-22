using FluentValidation;
using MediatR;
using TracLabs.Application.Features.Clients.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Clients.Commands.UpdateClient;

public record UpdateClientCommand(
    Guid Id,
    string Email,
    string Phone,
    string ContactPerson,
    string Street,
    string City,
    string State,
    string PostalCode) : IRequest<ClientDto>;

public class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
{
    public UpdateClientCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty().WithMessage("O ID do cliente é obrigatório.");

        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail em formato inválido.")
            .MaximumLength(100).WithMessage("O e-mail não pode exceder 100 caracteres.");

        RuleFor(c => c.Phone)
            .NotEmpty().WithMessage("O telefone é obrigatório.")
            .MaximumLength(30).WithMessage("O telefone não pode exceder 30 caracteres.");
    }
}

public class UpdateClientCommandHandler : IRequestHandler<UpdateClientCommand, ClientDto>
{
    private readonly IClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateClientCommandHandler(IClientRepository clientRepository, IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ClientDto> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetByIdAsync(request.Id, cancellationToken);
        if (client == null)
            throw new KeyNotFoundException($"Cliente com ID '{request.Id}' não encontrado.");

        client.UpdateContactInfo(request.Email, request.Phone, request.ContactPerson);
        client.UpdateAddress(request.Street, request.City, request.State, request.PostalCode);

        _clientRepository.Update(client);
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
