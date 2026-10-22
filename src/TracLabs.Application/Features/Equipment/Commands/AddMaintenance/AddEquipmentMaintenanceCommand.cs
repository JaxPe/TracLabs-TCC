using FluentValidation;
using MediatR;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Equipment.Commands.AddMaintenance;

public record AddEquipmentMaintenanceCommand(
    Guid EquipmentId,
    DateTime Date,
    MaintenanceType Type,
    string Description,
    string PerformedBy,
    string? ResultSummary = null) : IRequest<MaintenanceRecordDto>;

public class AddEquipmentMaintenanceCommandValidator : AbstractValidator<AddEquipmentMaintenanceCommand>
{
    public AddEquipmentMaintenanceCommandValidator()
    {
        RuleFor(x => x.EquipmentId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.PerformedBy).NotEmpty().MaximumLength(200);
    }
}

public class AddEquipmentMaintenanceCommandHandler : IRequestHandler<AddEquipmentMaintenanceCommand, MaintenanceRecordDto>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddEquipmentMaintenanceCommandHandler(
        IEquipmentRepository equipmentRepository,
        IUnitOfWork unitOfWork)
    {
        _equipmentRepository = equipmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MaintenanceRecordDto> Handle(AddEquipmentMaintenanceCommand request, CancellationToken cancellationToken)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(request.EquipmentId, cancellationToken);
        if (equipment == null)
            throw new KeyNotFoundException($"Equipamento com ID '{request.EquipmentId}' não encontrado.");

        var record = equipment.AddMaintenanceRecord(
            request.Date,
            request.Type,
            request.Description,
            request.PerformedBy,
            request.ResultSummary);

        _equipmentRepository.Update(equipment);
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
