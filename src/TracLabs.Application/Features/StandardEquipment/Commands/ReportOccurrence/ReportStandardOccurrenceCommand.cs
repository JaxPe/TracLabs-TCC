using FluentValidation;
using MediatR;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.StandardEquipment.Commands.ReportOccurrence;

public record ReportStandardOccurrenceCommand(
    Guid StandardEquipmentId,
    DateTime Date,
    OccurrenceType Type,
    string Description,
    string ActionTaken,
    string ReportedBy,
    DateTime? ResolvedDate = null) : IRequest<OccurrenceRecordDto>;

public class ReportStandardOccurrenceCommandValidator : AbstractValidator<ReportStandardOccurrenceCommand>
{
    public ReportStandardOccurrenceCommandValidator()
    {
        RuleFor(x => x.StandardEquipmentId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ActionTaken).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ReportedBy).NotEmpty().MaximumLength(200);
    }
}

public class ReportStandardOccurrenceCommandHandler : IRequestHandler<ReportStandardOccurrenceCommand, OccurrenceRecordDto>
{
    private readonly IStandardEquipmentRepository _standardRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReportStandardOccurrenceCommandHandler(
        IStandardEquipmentRepository standardRepository,
        IUnitOfWork unitOfWork)
    {
        _standardRepository = standardRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OccurrenceRecordDto> Handle(ReportStandardOccurrenceCommand request, CancellationToken cancellationToken)
    {
        var standard = await _standardRepository.GetByIdAsync(request.StandardEquipmentId, cancellationToken);
        if (standard == null)
            throw new KeyNotFoundException($"Equipamento padrão com ID '{request.StandardEquipmentId}' não encontrado.");

        var record = standard.ReportOccurrence(
            request.Date,
            request.Type,
            request.Description,
            request.ActionTaken,
            request.ReportedBy,
            request.ResolvedDate);

        _standardRepository.Update(standard);
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
