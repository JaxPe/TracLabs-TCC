using TracLabs.Domain.Enums;
using FluentValidation;
using MediatR;
using TracLabs.Application.Features.Equipment.Commands.CreateEquipment;
using TracLabs.Application.Features.Equipment.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using ValidationException = TracLabs.Application.Common.Exceptions.ValidationException;

namespace TracLabs.Application.Features.Equipment.Commands.UpdateEquipment;

public record UpdateEquipmentCommand(
    Guid Id,
    string LabTag,
    string Model,
    string Manufacturer,
    string SerialNumber,
    string InstrumentType,
    decimal? RangeMin,
    decimal? RangeMax,
    string Unit,
    decimal? Resolution,
    decimal? NominalCapacity = null,
    string? Name = null,
    string? SoftwareVersion = null,
    string? FirmwareVersion = null,
    string? CurrentLocation = null,
    string? AcceptanceCriteria = null,
    string? MaintenancePlan = null,
    string? InitialConditionNotes = null,
    EquipmentStatus? Status = null) : IRequest<EquipmentDetailDto>;

public class UpdateEquipmentCommandValidator : AbstractValidator<UpdateEquipmentCommand>
{
    public UpdateEquipmentCommandValidator()
    {
        RuleFor(e => e.Id).NotEmpty().WithMessage("O ID do equipamento é obrigatório.");

        RuleFor(e => e.LabTag)
            .NotEmpty().WithMessage("A identificação/tag do equipamento é obrigatória.")
            .MaximumLength(50);

        RuleFor(e => e.Model).NotEmpty().MaximumLength(100);
        RuleFor(e => e.Manufacturer).NotEmpty().MaximumLength(100);
        RuleFor(e => e.SerialNumber).NotEmpty().MaximumLength(100);
        RuleFor(e => e.InstrumentType).NotEmpty().MaximumLength(100);
        RuleFor(e => e.Unit).NotEmpty().MaximumLength(20);

        When(e => e.RangeMin.HasValue && e.RangeMax.HasValue, () =>
        {
            RuleFor(e => e.RangeMax!.Value)
                .GreaterThan(e => e.RangeMin!.Value)
                .WithMessage("O limite superior da faixa deve ser maior que o limite inferior.");
        });

        When(e => e.Resolution.HasValue, () =>
        {
            RuleFor(e => e.Resolution!.Value)
                .GreaterThan(0)
                .WithMessage("A resolução deve ser um valor positivo.");
        });

        When(e => e.NominalCapacity.HasValue, () =>
        {
            RuleFor(e => e.NominalCapacity!.Value)
                .GreaterThan(0)
                .WithMessage("A capacidade nominal deve ser um valor positivo.");
        });

        RuleFor(e => e)
            .Must(e => (e.RangeMin.HasValue && e.RangeMax.HasValue) || e.NominalCapacity.HasValue || (e.RangeMax.HasValue && e.RangeMax.Value > 0))
            .WithMessage("O equipamento deve possuir uma faixa de medição (RangeMin/RangeMax) ou uma Capacidade Nominal especificada (ex: balões volumétricos).");
    }
}

public class UpdateEquipmentCommandHandler : IRequestHandler<UpdateEquipmentCommand, EquipmentDetailDto>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEquipmentCommandHandler(
        IEquipmentRepository equipmentRepository,
        IUnitOfWork unitOfWork)
    {
        _equipmentRepository = equipmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EquipmentDetailDto> Handle(UpdateEquipmentCommand request, CancellationToken cancellationToken)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (equipment == null)
            throw new KeyNotFoundException($"Equipamento com ID '{request.Id}' não encontrado.");

        var existingWithTag = await _equipmentRepository.GetByLabTagAsync(request.LabTag, cancellationToken);
        if (existingWithTag != null && existingWithTag.Id != request.Id)
        {
            throw new ValidationException(nameof(request.LabTag),
                $"Já existe outro equipamento cadastrado com o Tag '{request.LabTag}'.");
        }

        equipment.UpdateDetails(
            request.LabTag,
            request.Name ?? request.InstrumentType,
            request.Model,
            request.Manufacturer,
            request.SerialNumber,
            request.InstrumentType,
            request.Unit,
            request.RangeMin,
            request.RangeMax,
            request.Resolution,
            request.NominalCapacity,
            request.SoftwareVersion,
            request.FirmwareVersion,
            request.CurrentLocation,
            request.AcceptanceCriteria,
            request.MaintenancePlan,
            request.InitialConditionNotes);

                if (request.Status.HasValue)
        {
            equipment.UpdateStatus(request.Status.Value);
        }

        _equipmentRepository.Update(equipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateEquipmentCommandHandler.MapToDetailDto(equipment, equipment.Client?.Name);
    }
}
