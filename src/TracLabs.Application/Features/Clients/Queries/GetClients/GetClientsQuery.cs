using MediatR;
using TracLabs.Application.Features.Clients.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Clients.Queries.GetClients;

public record GetClientsQuery(string? NameFilter = null) : IRequest<IReadOnlyList<ClientDto>>;

public class GetClientsQueryHandler : IRequestHandler<GetClientsQuery, IReadOnlyList<ClientDto>>
{
    private readonly IClientRepository _clientRepository;

    public GetClientsQueryHandler(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public async Task<IReadOnlyList<ClientDto>> Handle(GetClientsQuery request, CancellationToken cancellationToken)
    {
        var clients = string.IsNullOrWhiteSpace(request.NameFilter)
            ? await _clientRepository.GetAllAsync(cancellationToken)
            : await _clientRepository.SearchByNameAsync(request.NameFilter, cancellationToken);

        return clients
            .Select(c => new ClientDto(
                c.Id,
                c.Name,
                c.TaxId,
                c.TaxIdType,
                c.Email,
                c.Phone,
                c.ContactPerson,
                c.Street,
                c.City,
                c.State,
                c.PostalCode,
                !c.IsDeleted,
                c.CreatedAt))
            .ToList();
    }
}
