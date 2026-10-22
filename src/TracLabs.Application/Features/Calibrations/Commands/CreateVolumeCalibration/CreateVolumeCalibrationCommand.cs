using FluentValidation;
using MediatR;
using TracLabs.Application.DTOs.Calibration;
using TracLabs.Application.Features.Calibrations.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;

namespace TracLabs.Application.Features.Calibrations.Commands.CreateVolumeCalibration;

public record CreateVolumeCalibrationCommand(
    Guid EquipmentId,
    Guid StandardEquipmentId,
    DateTime NextCalibrationDue,
    decimal TemperatureCelsius,
    decimal HumidityPercent,
    string? Observations,
    VolumeCalibrationRequest CalculationRequest) : IRequest<CalibrationRecordDto>;

public class CreateVolumeCalibrationCommandValidator : AbstractValidator<CreateVolumeCalibrationCommand>
{
    public CreateVolumeCalibrationCommandValidator()
    {
        RuleFor(c => c.EquipmentId).NotEmpty().WithMessage("O equipamento do cliente deve ser informado.");
        RuleFor(c => c.StandardEquipmentId).NotEmpty().WithMessage("O equipamento padrão deve ser informado.");
        RuleFor(c => c.NextCalibrationDue).GreaterThan(DateTime.UtcNow)
            .WithMessage("A data da próxima calibração deve ser futura.");
        RuleFor(c => c.TemperatureCelsius).InclusiveBetween(10m, 35m)
            .WithMessage("A temperatura ambiental deve estar entre 10°C e 35°C.");
        RuleFor(c => c.HumidityPercent).InclusiveBetween(20m, 90m)
            .WithMessage("A umidade relativa deve estar entre 20% e 90%.");
        RuleFor(c => c.CalculationRequest).NotNull().WithMessage("Os dados de cálculo e pesagens são obrigatórios.");
    }
}

public class CreateVolumeCalibrationCommandHandler
    : IRequestHandler<CreateVolumeCalibrationCommand, CalibrationRecordDto>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IStandardEquipmentRepository _standardEquipmentRepository;
    private readonly ICalibrationRepository _calibrationRepository;
    private readonly IVolumeCalibrationService _volumeCalibrationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateVolumeCalibrationCommandHandler(
        IEquipmentRepository equipmentRepository,
        IStandardEquipmentRepository standardEquipmentRepository,
        ICalibrationRepository calibrationRepository,
        IVolumeCalibrationService volumeCalibrationService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _equipmentRepository = equipmentRepository;
        _standardEquipmentRepository = standardEquipmentRepository;
        _calibrationRepository = calibrationRepository;
        _volumeCalibrationService = volumeCalibrationService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CalibrationRecordDto> Handle(
        CreateVolumeCalibrationCommand request, CancellationToken cancellationToken)
    {
        // 1. Checagem do Equipamento no banco
        var equipment = await _equipmentRepository.GetByIdAsync(request.EquipmentId, cancellationToken);
        if (equipment == null)
            throw new KeyNotFoundException($"Equipamento do cliente com ID '{request.EquipmentId}' não encontrado.");

        if (equipment.Status != EquipmentStatus.Received && equipment.Status != EquipmentStatus.Approved)
        {
            throw new InvalidOperationException(
                $"Não é possível calibrar o equipamento '{equipment.LabTag}' no status atual '{equipment.Status}'.");
        }

        // 2. Checagem do Padrão no banco
        var standard = await _standardEquipmentRepository.GetByIdAsync(request.StandardEquipmentId, cancellationToken);
        if (standard == null)
            throw new KeyNotFoundException($"Equipamento padrão com ID '{request.StandardEquipmentId}' não encontrado.");

        if (!standard.IsUsableForCalibration)
        {
            throw new InvalidOperationException(
                $"O padrão '{standard.LabTag}' não está apto para uso metrológico. Status: '{standard.Status}', Validade: '{standard.CalibrationDueDate:dd/MM/yyyy}'.");
        }

        // 3. Execução do motor de cálculo volumétrico gravimétrico (ISO 4787 / DOC-CGCRE-027)
        var calcResult = _volumeCalibrationService.Calculate(request.CalculationRequest);

        // 4. Identificação do Técnico autenticado
        var technicianId = _currentUserService.UserId ?? "tech-system";
        var technicianName = _currentUserService.FullName ?? _currentUserService.Email ?? "Técnico Metrologista";

        // 5. Instanciação do Registro de Calibração
        var record = new CalibrationRecord(
            equipment.Id,
            standard.Id,
            technicianId,
            technicianName,
            DateTime.UtcNow,
            request.NextCalibrationDue,
            request.TemperatureCelsius,
            request.HumidityPercent);

        // 6. Conversão dos pontos de pesagem para Value Objects
        var measurementPoints = calcResult.CalculatedPoints
            .Select(p => new MeasurementPoint(calcResult.NominalVolumeMl, p.CalculatedVolumeMl, "mL"))
            .ToList();

        var maxAbsError = measurementPoints.Count > 0
            ? measurementPoints.Max(p => Math.Abs(p.Error))
            : Math.Abs(calcResult.SystematicErrorMl);

        record.RecordResults(
            measurementPoints,
            calcResult.SystematicErrorMl,
            maxAbsError,
            calcResult.CombinedStandardUncertaintyMl,
            calcResult.ExpandedUncertaintyMl,
            calcResult.CoverageFactor,
            CalibrationResult.Conforming,
            request.Observations);

        // 7. Atualização do ciclo de status do equipamento do cliente
        equipment.StartCalibration();
        equipment.CompleteCalibration(record);

        // 8. Persistência atômica no banco de dados
        await _calibrationRepository.AddAsync(record, cancellationToken);
        _equipmentRepository.Update(equipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CalibrationRecordDto(
            record.Id,
            equipment.Id,
            equipment.LabTag,
            equipment.Name,
            standard.Id,
            standard.LabTag,
            record.TechnicianId,
            record.TechnicianName,
            record.CalibrationDate,
            record.NextCalibrationDue,
            record.TemperatureCelsius,
            record.HumidityPercent,
            record.MeanError,
            record.MaxAbsoluteError,
            record.CombinedStandardUncertainty,
            record.ExpandedUncertainty,
            record.CoverageFactor,
            record.Result,
            record.Observations,
            record.MeasurementPoints,
            calcResult,
            record.Certificate?.CertificateNumber);
    }
}
