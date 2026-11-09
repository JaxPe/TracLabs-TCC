using FluentValidation;
using MediatR;
using TracLabs.Application.Features.Equipment.Commands.CreateEquipment;
using TracLabs.Application.Features.Equipment.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Equipment.Commands.UpdateEquipmentStatus;

public class UpdateEquipmentStatusRequest
{
    public EquipmentStatus? Status { get; set; }
    public EquipmentStatus? NewStatus { get; set; }
    public EquipmentStatus Value => NewStatus ?? Status ?? EquipmentStatus.Received;

    public UpdateEquipmentStatusRequest() { }
    public UpdateEquipmentStatusRequest(EquipmentStatus status)
    {
        Status = status;
        NewStatus = status;
    }
}

public record UpdateEquipmentStatusCommand(
    Guid EquipmentId,
    EquipmentStatus NewStatus
) : IRequest<EquipmentDetailDto>;

public class UpdateEquipmentStatusCommandValidator : AbstractValidator<UpdateEquipmentStatusCommand>
{
    public UpdateEquipmentStatusCommandValidator()
    {
        RuleFor(x => x.EquipmentId)
            .NotEmpty()
            .WithMessage("O ID do equipamento é obrigatório.");

        RuleFor(x => x.NewStatus)
            .IsInEnum()
            .WithMessage("O status informado é inválido.");
    }
}

public class UpdateEquipmentStatusCommandHandler : IRequestHandler<UpdateEquipmentStatusCommand, EquipmentDetailDto>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEquipmentStatusCommandHandler(
        IEquipmentRepository equipmentRepository,
        IUnitOfWork unitOfWork)
    {
        _equipmentRepository = equipmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EquipmentDetailDto> Handle(UpdateEquipmentStatusCommand request, CancellationToken cancellationToken)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(request.EquipmentId, cancellationToken);
        if (equipment == null)
            throw new KeyNotFoundException($"Equipamento com ID '{request.EquipmentId}' não encontrado.");

        equipment.UpdateStatus(request.NewStatus);
        _equipmentRepository.Update(equipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateEquipmentCommandHandler.MapToDetailDto(equipment, equipment.Client?.Name);
    }
}
