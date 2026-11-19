using MediatR;
using TracLabs.Application.Features.Clients.DTOs;
using TracLabs.Application.Features.Equipment.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Clients.Queries.GetClientWithEquipment;

public record GetClientWithEquipmentQuery(Guid Id) : IRequest<ClientWithEquipmentDto>;

public class GetClientWithEquipmentQueryHandler : IRequestHandler<GetClientWithEquipmentQuery, ClientWithEquipmentDto>
{
    private readonly IClientRepository _clientRepository;

    public GetClientWithEquipmentQueryHandler(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public async Task<ClientWithEquipmentDto> Handle(GetClientWithEquipmentQuery request, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetWithEquipmentAsync(request.Id, cancellationToken);
        if (client == null)
            throw new KeyNotFoundException($"Cliente com ID '{request.Id}' não encontrado.");

        var equipmentList = client.Equipment
            .Select(e => new EquipmentSummaryDto(
                e.Id,
                e.LabTag,
                e.Name,
                e.Model,
                e.Manufacturer,
                e.SerialNumber,
                e.InstrumentType,
                e.Status,
                e.CurrentLocation,
                client.Id,
                client.Name,
                e.NominalCapacity,
                e.Unit,
                e.RangeMin,
                e.RangeMax))
            .ToList();

        return new ClientWithEquipmentDto(
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
            client.CreatedAt,
            equipmentList);
    }
}
