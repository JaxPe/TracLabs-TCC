using FluentValidation;
using MediatR;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Equipment.Commands.ReportOccurrence;

public record ReportEquipmentOccurrenceCommand(
    Guid EquipmentId,
    DateTime Date,
    OccurrenceType Type,
    string Description,
    string ActionTaken,
    string ReportedBy,
    DateTime? ResolvedDate = null) : IRequest<OccurrenceRecordDto>;

public class ReportEquipmentOccurrenceCommandValidator : AbstractValidator<ReportEquipmentOccurrenceCommand>
{
    public ReportEquipmentOccurrenceCommandValidator()
    {
        RuleFor(x => x.EquipmentId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ActionTaken).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ReportedBy).NotEmpty().MaximumLength(200);
    }
}

public class ReportEquipmentOccurrenceCommandHandler : IRequestHandler<ReportEquipmentOccurrenceCommand, OccurrenceRecordDto>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReportEquipmentOccurrenceCommandHandler(
        IEquipmentRepository equipmentRepository,
        IUnitOfWork unitOfWork)
    {
        _equipmentRepository = equipmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OccurrenceRecordDto> Handle(ReportEquipmentOccurrenceCommand request, CancellationToken cancellationToken)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(request.EquipmentId, cancellationToken);
        if (equipment == null)
            throw new KeyNotFoundException($"Equipamento com ID '{request.EquipmentId}' não encontrado.");

        var record = equipment.ReportOccurrence(
            request.Date,
            request.Type,
            request.Description,
            request.ActionTaken,
            request.ReportedBy,
            request.ResolvedDate);

        _equipmentRepository.Update(equipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OccurrenceRecordDto(
            record.Id,
            record.OccurrenceDate,
            record.Type,
            record.Description,
            record.ActionTaken,
            record.ReportedBy,
            record.ResolvedDate);
    }
}
