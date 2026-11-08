using FluentValidation;
using MediatR;
using TracLabs.Application.Features.StandardEquipment.Commands.CreateStandardEquipment;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using ValidationException = TracLabs.Application.Common.Exceptions.ValidationException;

namespace TracLabs.Application.Features.StandardEquipment.Commands.UpdateStandardEquipment;

public record UpdateStandardEquipmentCommand(
    Guid Id,
    string LabTag,
    string Model,
    string Manufacturer,
    string SerialNumber,
    string InstrumentType,
    decimal Resolution,
    decimal RangeMin,
    decimal RangeMax,
    int CalibrationIntervalMonths = 12,
    string? Name = null,
    string? SoftwareVersion = null,
    string? FirmwareVersion = null,
    string? CurrentLocation = null,
    string? AcceptanceCriteria = null,
    string? MaintenancePlan = null,
    string? ReferenceMaterialDocumentation = null,
    StandardRole? Role = null) : IRequest<StandardEquipmentDto>;

public class UpdateStandardEquipmentCommandValidator : AbstractValidator<UpdateStandardEquipmentCommand>
{
    public UpdateStandardEquipmentCommandValidator()
    {
        RuleFor(s => s.Id).NotEmpty().WithMessage("O ID do equipamento padrão é obrigatório.");

        RuleFor(s => s.LabTag)
            .NotEmpty().WithMessage("A identificação/tag do padrão é obrigatória.")
            .MaximumLength(50);

        RuleFor(s => s.Model).MaximumLength(100);
        RuleFor(s => s.Manufacturer).MaximumLength(100);
        RuleFor(s => s.SerialNumber).MaximumLength(100);
        RuleFor(s => s.InstrumentType).MaximumLength(100);

        RuleFor(s => s.Resolution)
            .GreaterThanOrEqualTo(0)
            .WithMessage("A resolução não pode ser negativa.");

        RuleFor(s => s.RangeMax)
            .GreaterThanOrEqualTo(s => s.RangeMin)
            .WithMessage("O limite superior da faixa física (RangeMax) deve ser maior ou igual ao inferior (RangeMin).");

        RuleFor(s => s.CalibrationIntervalMonths)
            .GreaterThan(0)
            .WithMessage("O intervalo de calibração deve ser maior que zero.");
    }
}

public class UpdateStandardEquipmentCommandHandler : IRequestHandler<UpdateStandardEquipmentCommand, StandardEquipmentDto>
{
    private readonly IStandardEquipmentRepository _standardRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStandardEquipmentCommandHandler(
        IStandardEquipmentRepository standardRepository,
        IUnitOfWork unitOfWork)
    {
        _standardRepository = standardRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StandardEquipmentDto> Handle(UpdateStandardEquipmentCommand request, CancellationToken cancellationToken)
    {
        var standard = await _standardRepository.GetWithCertificatesAsync(request.Id, cancellationToken);
        if (standard == null)
            throw new KeyNotFoundException($"Equipamento padrão com ID '{request.Id}' não encontrado.");

        var existing = await _standardRepository.GetByLabTagAsync(request.LabTag, cancellationToken);
        if (existing != null && existing.Id != request.Id)
        {
            throw new ValidationException(nameof(request.LabTag),
                $"Já existe outro padrão cadastrado com o Tag '{request.LabTag}'.");
        }

        standard.UpdateDetails(
            request.LabTag,
            request.Name ?? request.InstrumentType,
            request.Model,
            request.Manufacturer,
            request.SerialNumber,
            request.InstrumentType,
            request.Resolution,
            request.RangeMin,
            request.RangeMax,
            request.CalibrationIntervalMonths,
            request.SoftwareVersion,
            request.FirmwareVersion,
            request.CurrentLocation,
            request.AcceptanceCriteria,
            request.MaintenancePlan,
            request.ReferenceMaterialDocumentation,
            request.Role);

        _standardRepository.Update(standard);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateStandardEquipmentCommandHandler.MapToDto(standard);
    }
}
