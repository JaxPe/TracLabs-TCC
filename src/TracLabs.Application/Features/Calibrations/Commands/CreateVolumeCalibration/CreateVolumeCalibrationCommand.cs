using FluentValidation;
using MediatR;
using TracLabs.Application.DTOs.Calibration;
using TracLabs.Application.Features.Calibrations.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Application.Interfaces.Services;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;

namespace TracLabs.Application.Features.Calibrations.Commands.CreateVolumeCalibration;

public record CreateVolumeCalibrationCommand(
    Guid EquipmentId,
    Guid BalanceStandardId = default,
    Guid LiquidThermometerStandardId = default,
    Guid? AmbientThermometerStandardId = null,
    Guid BarometerStandardId = default,
    Guid? HygrometerStandardId = null,
    Guid? ThermoHygrometerStandardId = null,
    DateTime NextCalibrationDue = default,
    decimal NominalVolumeMl = 0m,
    VolumetricMaterial Material = VolumetricMaterial.BorosilicateGlass33,
    List<VolumeMeasurementInputDto>? Measurements = null,
    string? Observations = null,
    decimal? ThermalExpansionCoefficient = null,
    decimal? AcceptanceCriteria = null,
    // Parâmetros opcionais para compatibilidade retroativa
    Guid? StandardEquipmentId = null,
    decimal TemperatureCelsius = 20.0m,
    decimal HumidityPercent = 50.0m,
    VolumeCalibrationRequest? CalculationRequest = null) : IRequest<CalibrationRecordDto>;

public class CreateVolumeCalibrationCommandValidator : AbstractValidator<CreateVolumeCalibrationCommand>
{
    public CreateVolumeCalibrationCommandValidator()
    {
        RuleFor(c => c.EquipmentId).NotEmpty().WithMessage("O equipamento do cliente deve ser informado.");

        // O padrão da balança pode vir em BalanceStandardId ou StandardEquipmentId
        RuleFor(c => c)
            .Must(c => c.BalanceStandardId != Guid.Empty || (c.StandardEquipmentId.HasValue && c.StandardEquipmentId.Value != Guid.Empty))
            .WithMessage("O equipamento padrão de balança analítica deve ser informado.");

        RuleFor(c => c.LiquidThermometerStandardId)
            .NotEmpty().WithMessage("O equipamento padrão de termômetro de imersão (água/líquido) deve ser informado.");

        RuleFor(c => c.BarometerStandardId)
            .NotEmpty().WithMessage("O equipamento padrão de barômetro deve ser informado.");

        RuleFor(c => c)
            .Must(HaveValidAmbientStandards)
            .WithMessage("É obrigatório informar o Termômetro Ambiente e o Higrômetro, ou um Termo-higrômetro único.");

        RuleFor(c => c.NextCalibrationDue)
            .Must(due => due == default || due > DateTime.UtcNow)
            .WithMessage("A data da próxima calibração deve ser futura.");

        // Validação das repetições: mínimo 7 pontos conforme ABNT NBR ISO 4787 e DOC-CGCRE-027
        RuleFor(c => c)
            .Must(HaveValidMeasurements)
            .WithMessage("A calibração volumétrica exige no mínimo 7 pontos de medição (repetições) para assegurar a confiabilidade estatística e graus de liberdade adequados conforme a ABNT NBR ISO 4787 e DOC-CGCRE-027.");
    }

    private static bool HaveValidAmbientStandards(CreateVolumeCalibrationCommand c)
    {
        if (c.ThermoHygrometerStandardId.HasValue && c.ThermoHygrometerStandardId.Value != Guid.Empty)
            return true;

        var hasAmb = c.AmbientThermometerStandardId.HasValue && c.AmbientThermometerStandardId.Value != Guid.Empty;
        var hasHyg = c.HygrometerStandardId.HasValue && c.HygrometerStandardId.Value != Guid.Empty;

        return hasAmb && hasHyg;
    }

    private static bool HaveValidMeasurements(CreateVolumeCalibrationCommand c)
    {
        if (c.Measurements != null && c.Measurements.Count >= 7)
            return true;

        if (c.CalculationRequest?.Measurements != null && c.CalculationRequest.Measurements.Count >= 7)
            return true;

        return false;
    }
}

public class CreateVolumeCalibrationCommandHandler
    : IRequestHandler<CreateVolumeCalibrationCommand, CalibrationRecordDto>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IVolumeStandardResolverService _standardResolver;
    private readonly ICalibrationRepository _calibrationRepository;
    private readonly ICertificateRepository _certificateRepository;
    private readonly IVolumeCalibrationService _volumeCalibrationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateVolumeCalibrationCommandHandler(
        IEquipmentRepository equipmentRepository,
        IVolumeStandardResolverService standardResolver,
        ICalibrationRepository calibrationRepository,
        ICertificateRepository certificateRepository,
        IVolumeCalibrationService volumeCalibrationService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _equipmentRepository = equipmentRepository;
        _standardResolver = standardResolver;
        _calibrationRepository = calibrationRepository;
        _certificateRepository = certificateRepository;
        _volumeCalibrationService = volumeCalibrationService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CalibrationRecordDto> Handle(
        CreateVolumeCalibrationCommand request, CancellationToken cancellationToken)
    {
        var warnings = new List<string>();

        // 1. Checagem do Equipamento do Cliente
        var equipment = await _equipmentRepository.GetByIdAsync(request.EquipmentId, cancellationToken);
        if (equipment == null)
            throw new KeyNotFoundException($"Equipamento do cliente com ID '{request.EquipmentId}' não encontrado.");

        if (equipment.Status != EquipmentStatus.Received && equipment.Status != EquipmentStatus.Approved)
        {
            throw new InvalidOperationException(
                $"Não é possível calibrar o equipamento '{equipment.LabTag}' no status atual '{equipment.Status}'.");
        }

        // Validação para micropipetas / aparelhos a pistão: mínimo de 10 repetições conforme ISO 8655-6
        var measurementsList = request.Measurements ?? request.CalculationRequest?.Measurements ?? new List<VolumeMeasurementInputDto>();
        var isPistonInstrument = equipment.InstrumentType.Contains("pipeta", StringComparison.OrdinalIgnoreCase) ||
                                 equipment.InstrumentType.Contains("dispensador", StringComparison.OrdinalIgnoreCase);

        if (isPistonInstrument && measurementsList.Count < 10)
        {
            throw new InvalidOperationException(
                $"Para calibração de instrumentos de pistão / microvolume ('{equipment.InstrumentType}'), a norma ABNT NBR ISO 8655-6 e o DOC-CGCRE-027 exigem no mínimo 10 repetições.");
        }

        var nominalVol = request.NominalVolumeMl > 0 ? request.NominalVolumeMl : equipment.RangeMax;
        if (!equipment.IsWithinCalibrationRange(nominalVol))
        {
            warnings.Add($"O volume nominal de ensaio ({nominalVol} mL) do equipamento '{equipment.LabTag}' está fora da margem de calibração ({equipment.CalibrationRangeMin} a {equipment.CalibrationRangeMax} mL).");
        }

        // 2. Resolução dos Padrões Metrológicos com Certificados via Serviço Centralizado (DRY)
        var balanceId = request.BalanceStandardId != Guid.Empty
            ? request.BalanceStandardId
            : (request.StandardEquipmentId ?? Guid.Empty);

        var resolutionRequest = new VolumeStandardsResolutionRequest(
            nominalVol,
            request.Material,
            measurementsList,
            balanceId,
            request.LiquidThermometerStandardId,
            request.BarometerStandardId,
            request.AmbientThermometerStandardId,
            request.HygrometerStandardId,
            request.ThermoHygrometerStandardId,
            request.ThermalExpansionCoefficient,
            request.CalculationRequest?.MeniscusSemiIntervalMl);

        var resolution = await _standardResolver.ResolveAsync(resolutionRequest, cancellationToken);
        if (resolution.Warnings.Count > 0)
        {
            warnings.AddRange(resolution.Warnings);
        }

        // 3. Execução do Motor Metrológico
        var calcResult = _volumeCalibrationService.Calculate(resolution.CalculationRequest);

        // 4. Criação do Registro de Calibração
        var technicianId = _currentUserService.UserId ?? "SYSTEM_TECH";
        var technicianName = _currentUserService.FullName ?? "Técnico Metrologista";
        var nextDue = request.NextCalibrationDue != default
            ? request.NextCalibrationDue
            : DateTime.UtcNow.AddYears(1);

        var tempAmb = measurementsList.Count > 0
            ? measurementsList.Average(m => m.AmbientTemperatureCelsius > 0 ? m.AmbientTemperatureCelsius : request.TemperatureCelsius)
            : request.TemperatureCelsius;

        var humAmb = measurementsList.Count > 0
            ? measurementsList.Average(m => m.RelativeHumidityPercent > 0 ? m.RelativeHumidityPercent : request.HumidityPercent)
            : request.HumidityPercent;

        var record = new CalibrationRecord(
            equipment.Id,
            resolution.Balance.Id,
            technicianId,
            technicianName,
            DateTime.UtcNow,
            nextDue,
            tempAmb,
            humAmb);

        // 5. Congelamento dos Snapshots dos Padrões (Rastreabilidade ISO 17025)
        var standardsUsed = resolution.BuildStandardsUsed(record.Id);
        record.AddStandardsUsed(standardsUsed);

        // 6. Gravação dos Resultados Metrológicos
        var measurementPoints = calcResult.CalculatedPoints
            .Select(p => new MeasurementPoint(calcResult.NominalVolumeMl, p.CalculatedVolumeMl, "mL"))
            .ToList();

        var maxAbsError = measurementPoints.Count > 0
            ? measurementPoints.Max(p => Math.Abs(p.Error))
            : Math.Abs(calcResult.SystematicErrorMl);

        // Conformidade com critérios de aceitação
        var isConforming = request.AcceptanceCriteria.HasValue
            ? maxAbsError <= request.AcceptanceCriteria.Value
            : true;

        var result = isConforming ? CalibrationResult.Conforming : CalibrationResult.NonConforming;

        var effectiveObservations = request.Observations;
        if (warnings.Count > 0)
        {
            var warningsText = "[Avisos Metrológicos]: " + string.Join("; ", warnings);
            effectiveObservations = string.IsNullOrWhiteSpace(effectiveObservations)
                ? warningsText
                : $"{effectiveObservations}\n{warningsText}";
        }

        var waterDensityGcm3 = calcResult.WaterDensityGramsPerCm3 > 0 ? calcResult.WaterDensityGramsPerCm3 : (decimal?)null;
        var airDensityGcm3 = calcResult.AirDensityGramsPerCm3 > 0 ? calcResult.AirDensityGramsPerCm3 : (decimal?)null;

        record.RecordResults(
            measurementPoints,
            calcResult.SystematicErrorMl,
            maxAbsError,
            calcResult.CombinedStandardUncertaintyMl,
            calcResult.ExpandedUncertaintyMl,
            calcResult.CoverageFactor,
            result,
            effectiveObservations,
            calcResult.EffectiveDegreesOfFreedom,
            waterDensityGcm3,
            airDensityGcm3);

        // 7. Persistência atômica e transacional (ISO 17025 e integridade ACID)
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 7.1. Geração atômica da numeração sequencial do certificado com lock pessimista
            var nextCertNumber = await _certificateRepository.GenerateNextCertificateNumberAsync(cancellationToken);
            var certificate = new Certificate(nextCertNumber, record.Id);
            record.AttachCertificate(certificate);

            // 7.2. Registro explícito das novas entidades no ChangeTracker
            await _calibrationRepository.AddAsync(record, cancellationToken);
            await _certificateRepository.AddAsync(certificate, cancellationToken);

            // 7.3. Atualização do ciclo de status do equipamento do cliente
            equipment.StartCalibration();
            equipment.CompleteCalibration(record);
            _equipmentRepository.Update(equipment);

            // 7.4. Flush atômico e confirmação da transação
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var standardsUsedDtos = standardsUsed.Select(s => new CalibrationStandardUsedDto(
            s.StandardEquipmentId,
            s.Role,
            s.LabTag,
            s.SerialNumber,
            s.CertificateNumber,
            s.Resolution,
            s.Correction,
            s.ExpandedUncertainty,
            s.CoverageFactor,
            s.OperatingValue,
            s.IsExtrapolated,
            s.WarningMessage,
            s.CertificateDueDate)).ToList();

        return new CalibrationRecordDto(
            record.Id,
            equipment.Id,
            equipment.LabTag,
            equipment.Name,
            resolution.Balance.Id,
            resolution.Balance.LabTag,
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
            record.Certificate?.CertificateNumber,
            standardsUsedDtos,
            warnings.Count > 0 ? warnings : null);
    }
}
