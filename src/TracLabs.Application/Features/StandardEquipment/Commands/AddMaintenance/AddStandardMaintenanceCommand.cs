using FluentValidation;
using MediatR;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.StandardEquipment.Commands.AddMaintenance;

public record AddStandardMaintenanceCommand(
    Guid StandardEquipmentId,
    DateTime Date,
    MaintenanceType Type,
    string Description,
    string PerformedBy,
    string? ResultSummary = null) : IRequest<MaintenanceRecordDto>;

public class AddStandardMaintenanceCommandValidator : AbstractValidator<AddStandardMaintenanceCommand>
{
    public AddStandardMaintenanceCommandValidator()
    {
        RuleFor(x => x.StandardEquipmentId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.PerformedBy).NotEmpty().MaximumLength(200);
    }
}

public class AddStandardMaintenanceCommandHandler : IRequestHandler<AddStandardMaintenanceCommand, MaintenanceRecordDto>
{
    private readonly IStandardEquipmentRepository _standardRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddStandardMaintenanceCommandHandler(
        IStandardEquipmentRepository standardRepository,
        IUnitOfWork unitOfWork)
    {
        _standardRepository = standardRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MaintenanceRecordDto> Handle(AddStandardMaintenanceCommand request, CancellationToken cancellationToken)
    {
        var standard = await _standardRepository.GetByIdAsync(request.StandardEquipmentId, cancellationToken);
        if (standard == null)
            throw new KeyNotFoundException($"Equipamento padrão com ID '{request.StandardEquipmentId}' não encontrado.");

        var record = standard.AddMaintenanceRecord(
            request.Date,
            request.Type,
            request.Description,
            request.PerformedBy,
            request.ResultSummary);

        _standardRepository.Update(standard);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MaintenanceRecordDto(
            record.Id,
            record.MaintenanceDate,
            record.Type,
            record.Description,
            record.PerformedBy,
            record.ResultSummary);
    }
}
