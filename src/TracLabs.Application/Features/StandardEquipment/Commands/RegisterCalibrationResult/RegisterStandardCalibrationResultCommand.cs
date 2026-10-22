using FluentValidation;
using MediatR;
using TracLabs.Application.Features.StandardEquipment.Commands.CreateStandardEquipment;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.StandardEquipment.Commands.RegisterCalibrationResult;

public record RegisterStandardCalibrationResultCommand(
    Guid Id,
    decimal NewError,
    decimal NewExpandedUncertainty,
    decimal CoverageFactor,
    DateTime CalibrationDate,
    DateTime DueDate,
    string NewCertificateNumber,
    string CalibratingLaboratory,
    decimal? NewResolution = null,
    bool MeetsAcceptanceCriteria = true) : IRequest<StandardEquipmentDto>;

public class RegisterStandardCalibrationResultCommandValidator : AbstractValidator<RegisterStandardCalibrationResultCommand>
{
    public RegisterStandardCalibrationResultCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CoverageFactor).GreaterThan(0);
        RuleFor(x => x.DueDate).GreaterThan(x => x.CalibrationDate);
        RuleFor(x => x.NewCertificateNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CalibratingLaboratory).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NewResolution).GreaterThanOrEqualTo(0).When(x => x.NewResolution.HasValue);
    }
}

public class RegisterStandardCalibrationResultCommandHandler : IRequestHandler<RegisterStandardCalibrationResultCommand, StandardEquipmentDto>
{
    private readonly IStandardEquipmentRepository _standardRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterStandardCalibrationResultCommandHandler(
        IStandardEquipmentRepository standardRepository,
        IUnitOfWork unitOfWork)
    {
        _standardRepository = standardRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StandardEquipmentDto> Handle(RegisterStandardCalibrationResultCommand request, CancellationToken cancellationToken)
    {
        var standard = await _standardRepository.GetByIdAsync(request.Id, cancellationToken);
        if (standard == null)
            throw new KeyNotFoundException($"Equipamento padrão com ID '{request.Id}' não encontrado.");

        standard.RegisterCalibrationResult(
            request.NewError,
            request.NewExpandedUncertainty,
            request.CoverageFactor,
            request.CalibrationDate,
            request.DueDate,
            request.NewCertificateNumber,
            request.CalibratingLaboratory,
            request.NewResolution,
            request.MeetsAcceptanceCriteria);

        _standardRepository.Update(standard);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateStandardEquipmentCommandHandler.MapToDto(standard);
    }
}
