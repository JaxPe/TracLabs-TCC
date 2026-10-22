using FluentValidation;
using MediatR;
using TracLabs.Application.Features.Equipment.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using ValidationException = TracLabs.Application.Common.Exceptions.ValidationException;

namespace TracLabs.Application.Features.Equipment.Commands.CreateEquipment;

public record CreateEquipmentCommand(
    string LabTag,
    string Model,
    string Manufacturer,
    string SerialNumber,
    string InstrumentType,
    decimal RangeMin,
    decimal RangeMax,
    string Unit,
    decimal Resolution,
    Guid ClientId,
    string? Name = null,
    string? SoftwareVersion = null,
    string? FirmwareVersion = null,
    string? CurrentLocation = null,
    string? AcceptanceCriteria = null,
    string? MaintenancePlan = null,
    string? InitialConditionNotes = null) : IRequest<EquipmentDetailDto>;

public class CreateEquipmentCommandValidator : AbstractValidator<CreateEquipmentCommand>
{
    public CreateEquipmentCommandValidator()
    {
        RuleFor(e => e.LabTag)
            .NotEmpty().WithMessage("A identificação/tag do equipamento é obrigatória.")
            .MaximumLength(50);

        RuleFor(e => e.Model).NotEmpty().MaximumLength(100);
        RuleFor(e => e.Manufacturer).NotEmpty().MaximumLength(100);
        RuleFor(e => e.SerialNumber).NotEmpty().MaximumLength(100);
        RuleFor(e => e.InstrumentType).NotEmpty().MaximumLength(100);
        RuleFor(e => e.Unit).NotEmpty().MaximumLength(20);

        RuleFor(e => e.RangeMax)
            .GreaterThan(e => e.RangeMin)
            .WithMessage("O limite superior da faixa deve ser maior que o limite inferior.");

        RuleFor(e => e.Resolution)
            .GreaterThan(0)
            .WithMessage("A resolução deve ser um valor positivo.");

        RuleFor(e => e.ClientId).NotEmpty().WithMessage("O cliente proprietário deve ser informado.");
    }
}

public class CreateEquipmentCommandHandler : IRequestHandler<CreateEquipmentCommand, EquipmentDetailDto>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEquipmentCommandHandler(
        IEquipmentRepository equipmentRepository,
        IClientRepository clientRepository,
        IUnitOfWork unitOfWork)
    {
        _equipmentRepository = equipmentRepository;
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EquipmentDetailDto> Handle(CreateEquipmentCommand request, CancellationToken cancellationToken)
    {
        var existingTag = await _equipmentRepository.GetByLabTagAsync(request.LabTag, cancellationToken);
        if (existingTag != null)
        {
            throw new ValidationException(nameof(request.LabTag),
                $"Já existe um equipamento cadastrado com o Tag '{request.LabTag}'.");
        }

        var client = await _clientRepository.GetByIdAsync(request.ClientId, cancellationToken);
        if (client == null)
            throw new KeyNotFoundException($"Cliente com ID '{request.ClientId}' não encontrado.");

        var equipment = new TracLabs.Domain.Entities.Equipment(
            request.LabTag,
            request.Model,
            request.Manufacturer,
            request.SerialNumber,
            request.InstrumentType,
            request.RangeMin,
            request.RangeMax,
            request.Unit,
            request.Resolution,
            request.ClientId,
            request.Name,
            request.SoftwareVersion,
            request.FirmwareVersion,
            request.CurrentLocation,
            request.AcceptanceCriteria,
            request.MaintenancePlan,
            request.InitialConditionNotes);

        await _equipmentRepository.AddAsync(equipment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(equipment, client.Name);
    }

    public static EquipmentDetailDto MapToDetailDto(TracLabs.Domain.Entities.Equipment e, string? clientName = null)
    {
        return new EquipmentDetailDto(
            e.Id,
            e.LabTag,
            e.Name,
            e.Model,
            e.Manufacturer,
            e.SerialNumber,
            e.InstrumentType,
            e.RangeMin,
            e.RangeMax,
            e.Unit,
            e.Resolution,
            e.ClientId,
            clientName ?? e.Client?.Name,
            e.SoftwareVersion,
            e.FirmwareVersion,
            e.CurrentLocation,
            e.AcceptanceCriteria,
            e.MaintenancePlan,
            e.InitialConditionNotes,
            e.Status,
            e.CreatedAt);
    }
}
