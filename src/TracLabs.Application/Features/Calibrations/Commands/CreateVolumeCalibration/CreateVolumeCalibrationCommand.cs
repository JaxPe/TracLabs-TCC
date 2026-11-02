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
    Guid BalanceStandardId = default,
    Guid LiquidThermometerStandardId = default,
    Guid? AmbientThermometerStandardId = null,
    Guid BarometerStandardId = default,
    Guid HygrometerStandardId = default,
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

        RuleFor(c => c.NextCalibrationDue)
            .Must(due => due == default || due > DateTime.UtcNow)
            .WithMessage("A data da próxima calibração deve ser futura.");

        // Validação das repetições: mínimo 7 pontos conforme ABNT NBR ISO 4787 e DOC-CGCRE-027
        RuleFor(c => c)
            .Must(HaveValidMeasurements)
            .WithMessage("A calibração volumétrica exige no mínimo 7 pontos de medição (repetições) para assegurar a confiabilidade estatística e graus de liberdade adequados conforme a ABNT NBR ISO 4787 e DOC-CGCRE-027.");
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

        // 2. Resolução dos Padrões Metrológicos
        var balanceId = request.BalanceStandardId != Guid.Empty
            ? request.BalanceStandardId
            : (request.StandardEquipmentId ?? Guid.Empty);

        var balance = await _standardEquipmentRepository.GetByIdAsync(balanceId, cancellationToken);
        if (balance == null)
            throw new KeyNotFoundException($"Padrão de balança analítica com ID '{balanceId}' não encontrado.");

        EnsureStandardIsUsable(balance, "Balança Analítica");

        // Outros padrões (termômetros, barômetro, higrômetro)
        TracLabs.Domain.Entities.StandardEquipment? liquidThermometer = null;
        if (request.LiquidThermometerStandardId != Guid.Empty)
        {
            liquidThermometer = await _standardEquipmentRepository.GetByIdAsync(request.LiquidThermometerStandardId, cancellationToken);
            if (liquidThermometer == null)
                throw new KeyNotFoundException($"Padrão de termômetro de líquido com ID '{request.LiquidThermometerStandardId}' não encontrado.");
            EnsureStandardIsUsable(liquidThermometer, "Termômetro do Líquido");
        }

        TracLabs.Domain.Entities.StandardEquipment? ambientThermometer = null;
        if (request.AmbientThermometerStandardId.HasValue && request.AmbientThermometerStandardId.Value != Guid.Empty)
        {
            ambientThermometer = await _standardEquipmentRepository.GetByIdAsync(request.AmbientThermometerStandardId.Value, cancellationToken);
            if (ambientThermometer != null)
                EnsureStandardIsUsable(ambientThermometer, "Termômetro Ambiente");
        }

        TracLabs.Domain.Entities.StandardEquipment? barometer = null;
        if (request.BarometerStandardId != Guid.Empty)
        {
            barometer = await _standardEquipmentRepository.GetByIdAsync(request.BarometerStandardId, cancellationToken);
            if (barometer == null)
                throw new KeyNotFoundException($"Padrão de barômetro com ID '{request.BarometerStandardId}' não encontrado.");
            EnsureStandardIsUsable(barometer, "Barômetro");
        }

        TracLabs.Domain.Entities.StandardEquipment? hygrometer = null;
        if (request.HygrometerStandardId != Guid.Empty)
        {
            hygrometer = await _standardEquipmentRepository.GetByIdAsync(request.HygrometerStandardId, cancellationToken);
            if (hygrometer == null)
                throw new KeyNotFoundException($"Padrão de higrômetro com ID '{request.HygrometerStandardId}' não encontrado.");
            EnsureStandardIsUsable(hygrometer, "Higrômetro");
        }

        // 3. Montagem da Requisição de Cálculo (com dados extraídos automaticamente dos padrões)
        var calcReq = request.CalculationRequest ?? new VolumeCalibrationRequest
        {
            NominalVolumeMl = request.NominalVolumeMl > 0 ? request.NominalVolumeMl : equipment.RangeMax,
            Material = request.Material,
            ThermalExpansionCoefficient = request.ThermalExpansionCoefficient,
            Measurements = measurementsList,
            // Balança
            BalanceResolutionGrams = balance.Resolution,
            BalanceCertificateUncertaintyGrams = balance.ExpandedUncertainty,
            BalanceCoverageFactor = balance.CoverageFactor > 0 ? balance.CoverageFactor : 2.0m,
            BalanceFullCorrectionGrams = balance.Error,
            // Termômetro Líquido
            WaterThermometerResolutionCelsius = liquidThermometer?.Resolution ?? 0.01m,
            WaterThermometerUncertaintyCelsius = liquidThermometer?.ExpandedUncertainty ?? 0.02m,
            WaterThermometerCoverageFactor = liquidThermometer?.CoverageFactor ?? 2.0m,
            WaterThermometerCorrectionCelsius = liquidThermometer?.Error ?? 0m,
            // Termômetro Ambiente
            AmbientThermometerResolutionCelsius = ambientThermometer?.Resolution ?? liquidThermometer?.Resolution ?? 0.1m,
            AmbientThermometerUncertaintyCelsius = ambientThermometer?.ExpandedUncertainty ?? liquidThermometer?.ExpandedUncertainty ?? 0.05m,
            AmbientThermometerCoverageFactor = ambientThermometer?.CoverageFactor ?? 2.0m,
            AmbientThermometerCorrectionCelsius = ambientThermometer?.Error ?? 0m,
            // Barômetro
            BarometerResolutionHpa = barometer?.Resolution ?? 0.1m,
            BarometerUncertaintyHpa = barometer?.ExpandedUncertainty ?? 0.5m,
            BarometerCoverageFactor = barometer?.CoverageFactor ?? 2.0m,
            BarometerCorrectionHpa = barometer?.Error ?? 0m,
            // Higrômetro
            HygrometerResolutionPercent = hygrometer?.Resolution ?? 1.0m,
            HygrometerUncertaintyPercent = hygrometer?.ExpandedUncertainty ?? 2.0m,
            HygrometerCoverageFactor = hygrometer?.CoverageFactor ?? 2.0m,
            HygrometerCorrectionPercent = hygrometer?.Error ?? 0m
        };

        // 4. Execução do motor de cálculo volumétrico gravimétrico
        var calcResult = _volumeCalibrationService.Calculate(calcReq);

        // 5. Identificação do Técnico autenticado
        var technicianId = _currentUserService.UserId ?? "tech-system";
        var technicianName = _currentUserService.FullName ?? _currentUserService.Email ?? "Técnico Metrologista";

        var nextDue = request.NextCalibrationDue != default
            ? request.NextCalibrationDue
            : DateTime.UtcNow.AddMonths(12);

        var tempAmb = measurementsList.Count > 0 ? measurementsList.Average(m => m.AmbientTemperatureCelsius) : request.TemperatureCelsius;
        var humAmb = measurementsList.Count > 0 ? measurementsList.Average(m => m.RelativeHumidityPercent) : request.HumidityPercent;

        // 6. Instanciação do Registro de Calibração
        var record = new CalibrationRecord(
            equipment.Id,
            balance.Id,
            technicianId,
            technicianName,
            DateTime.UtcNow,
            nextDue,
            tempAmb,
            humAmb);

        // 7. Congelamento dos Snapshots dos Padrões (Rastreabilidade ISO 17025)
        var standardsUsed = new List<CalibrationStandardUsed>
        {
            new(record.Id, balance, StandardRole.Balance)
        };

        if (liquidThermometer != null)
            standardsUsed.Add(new(record.Id, liquidThermometer, StandardRole.LiquidThermometer));

        if (ambientThermometer != null && ambientThermometer.Id != liquidThermometer?.Id)
            standardsUsed.Add(new(record.Id, ambientThermometer, StandardRole.AmbientThermometer));

        if (barometer != null)
            standardsUsed.Add(new(record.Id, barometer, StandardRole.Barometer));

        if (hygrometer != null)
            standardsUsed.Add(new(record.Id, hygrometer, StandardRole.Hygrometer));

        record.AddStandardsUsed(standardsUsed);

        // 8. Gravação dos Resultados Metrológicos
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

        record.RecordResults(
            measurementPoints,
            calcResult.SystematicErrorMl,
            maxAbsError,
            calcResult.CombinedStandardUncertaintyMl,
            calcResult.ExpandedUncertaintyMl,
            calcResult.CoverageFactor,
            result,
            request.Observations);

        // 9. Atualização do ciclo de status do equipamento do cliente
        equipment.StartCalibration();
        equipment.CompleteCalibration(record);

        // 10. Persistência atômica no banco de dados
        await _calibrationRepository.AddAsync(record, cancellationToken);
        _equipmentRepository.Update(equipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var standardsUsedDtos = standardsUsed.Select(s => new CalibrationStandardUsedDto(
            s.StandardEquipmentId,
            s.Role,
            s.LabTag,
            s.SerialNumber,
            s.CertificateNumber,
            s.Resolution,
            s.Correction,
            s.ExpandedUncertainty,
            s.CoverageFactor)).ToList();

        return new CalibrationRecordDto(
            record.Id,
            equipment.Id,
            equipment.LabTag,
            equipment.Name,
            balance.Id,
            balance.LabTag,
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
            standardsUsedDtos);
    }

    private static void EnsureStandardIsUsable(TracLabs.Domain.Entities.StandardEquipment standard, string roleName)
    {
        if (!standard.IsUsableForCalibration)
        {
            throw new InvalidOperationException(
                $"O padrão '{standard.LabTag}' ({roleName}) não está apto para uso metrológico. Status: '{standard.Status}', Validade: '{standard.CalibrationDueDate:dd/MM/yyyy}'.");
        }
    }
}
