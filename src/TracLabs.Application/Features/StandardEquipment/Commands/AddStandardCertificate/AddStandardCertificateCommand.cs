using FluentValidation;
using MediatR;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;

namespace TracLabs.Application.Features.StandardEquipment.Commands.AddStandardCertificate;

public record AddStandardCertificateCommand(
    Guid StandardEquipmentId,
    string CertificateNumber,
    string CalibratingLaboratory,
    DateTime CalibrationDate,
    DateTime CalibrationDueDate,
    decimal CalibrationRangeMin,
    decimal CalibrationRangeMax,
    CurveFitType FitType = CurveFitType.LinearRegression,
    UncertaintyPolicy UncertaintyPolicy = UncertaintyPolicy.WorstCase,
    List<StandardCalibrationPointDto>? Points = null,
    bool IsActive = true) : IRequest<StandardCertificateDto>;

public class AddStandardCertificateCommandValidator : AbstractValidator<AddStandardCertificateCommand>
{
    public AddStandardCertificateCommandValidator()
    {
        RuleFor(c => c.StandardEquipmentId).NotEmpty().WithMessage("O ID do equipamento padrão deve ser informado.");
        RuleFor(c => c.CertificateNumber).NotEmpty().WithMessage("O número do certificado deve ser informado.");
        RuleFor(c => c.CalibratingLaboratory).NotEmpty().WithMessage("O laboratório calibrador deve ser informado.");
        RuleFor(c => c.CalibrationDueDate).GreaterThan(c => c.CalibrationDate)
            .WithMessage("A data de validade deve ser posterior à data de calibração.");
        RuleFor(c => c.CalibrationRangeMax).GreaterThanOrEqualTo(c => c.CalibrationRangeMin)
            .WithMessage("A margem máxima de calibração deve ser maior ou igual à margem mínima.");
    }
}

public class AddStandardCertificateCommandHandler : IRequestHandler<AddStandardCertificateCommand, StandardCertificateDto>
{
    private readonly IStandardEquipmentRepository _standardEquipmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddStandardCertificateCommandHandler(
        IStandardEquipmentRepository standardEquipmentRepository,
        IUnitOfWork unitOfWork)
    {
        _standardEquipmentRepository = standardEquipmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StandardCertificateDto> Handle(
        AddStandardCertificateCommand request,
        CancellationToken cancellationToken)
    {
        var standard = await _standardEquipmentRepository.GetWithCertificatesAsync(request.StandardEquipmentId, cancellationToken);
        if (standard == null)
            throw new KeyNotFoundException($"Equipamento padrão com ID '{request.StandardEquipmentId}' não encontrado.");

        var cert = new StandardCertificate(
            request.StandardEquipmentId,
            request.CertificateNumber,
            request.CalibratingLaboratory,
            request.CalibrationDate,
            request.CalibrationDueDate,
            request.CalibrationRangeMin,
            request.CalibrationRangeMax,
            request.FitType,
            request.UncertaintyPolicy,
            request.IsActive);

        if (request.Points != null && request.Points.Count > 0)
        {
            var points = request.Points.Select(p => new StandardCalibrationPoint(
                p.Order,
                p.IndicatedValue,
                p.StandardError,
                p.Correction,
                p.ExpandedUncertainty,
                p.CoverageFactor)).ToList();

            cert.SetPoints(points);
        }

        standard.AddCertificate(cert);
        _standardEquipmentRepository.Update(standard);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var pointsDto = cert.Points.Select(p => new StandardCalibrationPointDto(
            p.Order,
            p.IndicatedValue,
            p.StandardError,
            p.Correction,
            p.ExpandedUncertainty,
            p.CoverageFactor)).ToList();

        return new StandardCertificateDto(
            cert.Id,
            cert.StandardEquipmentId,
            cert.CertificateNumber,
            cert.CalibratingLaboratory,
            cert.CalibrationDate,
            cert.CalibrationDueDate,
            cert.CalibrationRangeMin,
            cert.CalibrationRangeMax,
            cert.IsActive,
            cert.FitType,
            cert.UncertaintyPolicy,
            cert.Slope,
            cert.Intercept,
            cert.R2,
            pointsDto);
    }
}
