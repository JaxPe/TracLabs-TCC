using MediatR;
using TracLabs.Application.Features.Clients.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Clients.Queries.GetClientById;

public record GetClientByIdQuery(Guid Id) : IRequest<ClientDto>;

public class GetClientByIdQueryHandler : IRequestHandler<GetClientByIdQuery, ClientDto>
{
    private readonly IClientRepository _clientRepository;

    public GetClientByIdQueryHandler(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public async Task<ClientDto> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetByIdAsync(request.Id, cancellationToken);
        if (client == null)
            throw new KeyNotFoundException($"Cliente com ID '{request.Id}' não encontrado.");

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
