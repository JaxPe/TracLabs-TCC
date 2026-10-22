using MediatR;
using TracLabs.Application.Features.Equipment.Commands.CreateEquipment;
using TracLabs.Application.Features.Equipment.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Equipment.Commands.UpdateLocation;

public record UpdateEquipmentLocationCommand(Guid Id, string NewLocation) : IRequest<EquipmentDetailDto>;

public class UpdateEquipmentLocationCommandHandler : IRequestHandler<UpdateEquipmentLocationCommand, EquipmentDetailDto>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEquipmentLocationCommandHandler(
        IEquipmentRepository equipmentRepository,
        IUnitOfWork unitOfWork)
    {
        _equipmentRepository = equipmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EquipmentDetailDto> Handle(UpdateEquipmentLocationCommand request, CancellationToken cancellationToken)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (equipment == null)
            throw new KeyNotFoundException($"Equipamento com ID '{request.Id}' não encontrado.");

        equipment.UpdateLocation(request.NewLocation);

        _equipmentRepository.Update(equipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateEquipmentCommandHandler.MapToDetailDto(equipment);
    }
}
