using MediatR;
using TracLabs.Application.Features.Equipment.Commands.CreateEquipment;
using TracLabs.Application.Features.Equipment.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Equipment.Queries.GetEquipmentById;

public record GetEquipmentByIdQuery(Guid Id) : IRequest<EquipmentDetailDto>;

public class GetEquipmentByIdQueryHandler : IRequestHandler<GetEquipmentByIdQuery, EquipmentDetailDto>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IClientRepository _clientRepository;

    public GetEquipmentByIdQueryHandler(
        IEquipmentRepository equipmentRepository,
        IClientRepository clientRepository)
    {
        _equipmentRepository = equipmentRepository;
        _clientRepository = clientRepository;
    }

    public async Task<EquipmentDetailDto> Handle(GetEquipmentByIdQuery request, CancellationToken cancellationToken)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (equipment == null)
            throw new KeyNotFoundException($"Equipamento com ID '{request.Id}' não encontrado.");

        var client = await _clientRepository.GetByIdAsync(equipment.ClientId, cancellationToken);

        return CreateEquipmentCommandHandler.MapToDetailDto(equipment, client?.Name);
    }
}
