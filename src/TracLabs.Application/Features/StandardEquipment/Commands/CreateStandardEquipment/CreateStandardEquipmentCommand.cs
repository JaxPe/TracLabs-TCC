using FluentValidation;
using MediatR;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using ValidationException = TracLabs.Application.Common.Exceptions.ValidationException;

namespace TracLabs.Application.Features.StandardEquipment.Commands.CreateStandardEquipment;

public record CreateStandardEquipmentCommand(
    string LabTag,
    string Model,
    string Manufacturer,
    string SerialNumber,
    string InstrumentType,
    decimal Error,
    decimal ExpandedUncertainty,
    decimal CoverageFactor,
    DateTime LastCalibrationDate,
    DateTime CalibrationDueDate,
    string CalibrationCertificateNumber,
    string CalibratingLaboratory,
    decimal Resolution = 0m,
    string? Name = null,
    string? SoftwareVersion = null,
    string? FirmwareVersion = null,
    string? CurrentLocation = null,
    string? AcceptanceCriteria = null,
    int CalibrationIntervalMonths = 12,
    string? ReferenceMaterialDocumentation = null,
    string? MaintenancePlan = null,
    decimal RangeMin = 0m,
    decimal RangeMax = 0m,
    decimal? CalibrationRangeMin = null,
    decimal? CalibrationRangeMax = null) : IRequest<StandardEquipmentDto>;

public class CreateStandardEquipmentCommandValidator : AbstractValidator<CreateStandardEquipmentCommand>
{
    public CreateStandardEquipmentCommandValidator()
    {
        RuleFor(s => s.LabTag)
            .NotEmpty().WithMessage("A identificação/tag do padrão é obrigatória.")
            .MaximumLength(50).WithMessage("O LabTag não pode exceder 50 caracteres.");

        RuleFor(s => s.Model)
            .NotEmpty().WithMessage("O modelo é obrigatório.")
            .MaximumLength(100);

        RuleFor(s => s.Manufacturer)
            .NotEmpty().WithMessage("O fabricante é obrigatório.")
            .MaximumLength(100);

        RuleFor(s => s.SerialNumber)
            .NotEmpty().WithMessage("O número de série é obrigatório.")
            .MaximumLength(100);

        RuleFor(s => s.InstrumentType)
            .NotEmpty().WithMessage("O tipo de instrumento é obrigatório.")
            .MaximumLength(100);

        RuleFor(s => s.CoverageFactor)
            .GreaterThan(0).WithMessage("O fator de abrangência k deve ser maior que zero.");

        RuleFor(s => s.CalibrationDueDate)
            .GreaterThan(s => s.LastCalibrationDate)
            .WithMessage("A data de validade da calibração deve ser posterior à data da última calibração.");

        RuleFor(s => s.CalibrationCertificateNumber)
            .NotEmpty().WithMessage("O número do certificado de calibração é obrigatório.")
            .MaximumLength(100);

        RuleFor(s => s.CalibratingLaboratory)
            .NotEmpty().WithMessage("O laboratório executor da calibração é obrigatório.")
            .MaximumLength(200);

        RuleFor(s => s.Resolution)
            .GreaterThanOrEqualTo(0).WithMessage("A resolução não pode ser negativa.");

        RuleFor(s => s.CalibrationIntervalMonths)
            .GreaterThan(0).WithMessage("O intervalo de calibração deve ser maior que zero.");
    }
}

public class CreateStandardEquipmentCommandHandler : IRequestHandler<CreateStandardEquipmentCommand, StandardEquipmentDto>
{
    private readonly IStandardEquipmentRepository _standardRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStandardEquipmentCommandHandler(
        IStandardEquipmentRepository standardRepository,
        IUnitOfWork unitOfWork)
    {
        _standardRepository = standardRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StandardEquipmentDto> Handle(CreateStandardEquipmentCommand request, CancellationToken cancellationToken)
    {
        var existing = await _standardRepository.GetByLabTagAsync(request.LabTag, cancellationToken);
        if (existing != null)
        {
            throw new ValidationException(nameof(request.LabTag),
                $"Já existe um padrão cadastrado com o Tag '{request.LabTag}'.");
        }

        var standard = new TracLabs.Domain.Entities.StandardEquipment(
            labTag: request.LabTag,
            model: request.Model,
            manufacturer: request.Manufacturer,
            serialNumber: request.SerialNumber,
            instrumentType: request.InstrumentType,
            error: request.Error,
            expandedUncertainty: request.ExpandedUncertainty,
            coverageFactor: request.CoverageFactor,
            lastCalibrationDate: request.LastCalibrationDate,
            calibrationDueDate: request.CalibrationDueDate,
            certificateNumber: request.CalibrationCertificateNumber,
            calibratingLaboratory: request.CalibratingLaboratory,
            resolution: request.Resolution,
            name: request.Name,
            softwareVersion: request.SoftwareVersion,
            firmwareVersion: request.FirmwareVersion,
            currentLocation: request.CurrentLocation,
            acceptanceCriteria: request.AcceptanceCriteria,
            calibrationIntervalMonths: request.CalibrationIntervalMonths,
            referenceMaterialDocumentation: request.ReferenceMaterialDocumentation,
            maintenancePlan: request.MaintenancePlan,
            status: StandardEquipmentStatus.Calibrated,
            rangeMin: request.RangeMin,
            rangeMax: request.RangeMax,
            calibrationRangeMin: request.CalibrationRangeMin,
            calibrationRangeMax: request.CalibrationRangeMax);

        await _standardRepository.AddAsync(standard, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(standard);
    }

    public static StandardEquipmentDto MapToDto(TracLabs.Domain.Entities.StandardEquipment s)
    {
        var certsDto = s.Certificates?.Select(c => new StandardCertificateDto(
            c.Id,
            c.StandardEquipmentId,
            c.CertificateNumber,
            c.CalibratingLaboratory,
            c.CalibrationDate,
            c.CalibrationDueDate,
            c.CalibrationRangeMin,
            c.CalibrationRangeMax,
            c.IsActive,
            c.FitType,
            c.UncertaintyPolicy,
            c.Slope,
            c.Intercept,
            c.R2,
            c.Points.Select(p => new StandardCalibrationPointDto(
                p.Order,
                p.IndicatedValue,
                p.StandardError,
                p.Correction,
                p.ExpandedUncertainty,
                p.CoverageFactor)).ToList())).ToList();

        return new StandardEquipmentDto(
            s.Id,
            s.LabTag,
            s.Name,
            s.Model,
            s.Manufacturer,
            s.SerialNumber,
            s.InstrumentType,
            s.SoftwareVersion,
            s.FirmwareVersion,
            s.CurrentLocation,
            s.AcceptanceCriteria,
            s.CalibrationIntervalMonths,
            s.ReferenceMaterialDocumentation,
            s.MaintenancePlan,
            s.Status,
            s.Error,
            s.Correction,
            s.ExpandedUncertainty,
            s.CoverageFactor,
            s.Resolution,
            s.LastCalibrationDate,
            s.CalibrationDueDate,
            s.CalibrationCertificateNumber,
            s.CalibratingLaboratory,
            s.IsCalibrationValid,
            s.DaysUntilExpiry,
            s.IsUsableForCalibration,
            s.CreatedAt,
            s.RangeMin,
            s.RangeMax,
            s.CalibrationRangeMin,
            s.CalibrationRangeMax,
            certsDto);
    }
}
