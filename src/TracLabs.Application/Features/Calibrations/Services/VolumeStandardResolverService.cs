using TracLabs.Application.DTOs.Calibration;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Application.Interfaces.Services;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;

namespace TracLabs.Application.Features.Calibrations.Services;

public class VolumeStandardResolverService : IVolumeStandardResolverService
{
    private readonly IStandardEquipmentRepository _standardEquipmentRepository;

    public VolumeStandardResolverService(IStandardEquipmentRepository standardEquipmentRepository)
    {
        _standardEquipmentRepository = standardEquipmentRepository;
    }

    public async Task<VolumeStandardsResolutionResult> ResolveAsync(
        VolumeStandardsResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Measurements == null || request.Measurements.Count == 0)
            throw new ArgumentException("A lista de medições gravimétricas não pode ser nula ou vazia.", nameof(request));

        if (request.Measurements.Count < 7)
            throw new ArgumentException("A calibração volumétrica exige no mínimo 7 pontos de medição (repetições) para assegurar a confiabilidade estatística e graus de liberdade adequados conforme a ABNT NBR ISO 4787 e DOC-CGCRE-027.", nameof(request));

        var warnings = new List<string>();

        // ── 1. Resolução dos IDs dos Padrões ──────────────────────────────────
        if (request.BalanceStandardId == Guid.Empty)
            throw new ArgumentException("O padrão de balança analítica é obrigatório para calibração volumétrica.", nameof(request));

        if (request.LiquidThermometerStandardId == Guid.Empty)
            throw new ArgumentException("O padrão de termômetro de imersão (para água/líquido) é obrigatório para calibração volumétrica.", nameof(request));

        if (request.BarometerStandardId == Guid.Empty)
            throw new ArgumentException("O padrão de barômetro é obrigatório para determinação da densidade do ar.", nameof(request));

        Guid effectiveAmbId = (request.AmbientThermometerStandardId.HasValue && request.AmbientThermometerStandardId.Value != Guid.Empty)
            ? request.AmbientThermometerStandardId.Value
            : (request.ThermoHygrometerStandardId ?? Guid.Empty);

        Guid effectiveHygId = (request.HygrometerStandardId.HasValue && request.HygrometerStandardId.Value != Guid.Empty)
            ? request.HygrometerStandardId.Value
            : (request.ThermoHygrometerStandardId ?? Guid.Empty);

        if (effectiveAmbId == Guid.Empty)
            throw new ArgumentException("O padrão de termômetro ambiente (ou termo-higrômetro) é obrigatório para calibração volumétrica.", nameof(request));

        if (effectiveHygId == Guid.Empty)
            throw new ArgumentException("O padrão de higrômetro (ou termo-higrômetro) é obrigatório para calibração volumétrica.", nameof(request));

        // ── 2. Carregamento e Validação de Aptidão dos Padrões (ISO 17025) ────
        var balance = await LoadAndValidateStandardAsync(request.BalanceStandardId, "Balança Analítica", cancellationToken);
        var liquidThermometer = await LoadAndValidateStandardAsync(request.LiquidThermometerStandardId, "Termômetro do Líquido", cancellationToken);
        var barometer = await LoadAndValidateStandardAsync(request.BarometerStandardId, "Barômetro", cancellationToken);

        TracLabs.Domain.Entities.StandardEquipment ambientThermometer;
        TracLabs.Domain.Entities.StandardEquipment hygrometer;

        if (effectiveAmbId == effectiveHygId)
        {
            var thermoHygrometer = await LoadAndValidateStandardAsync(effectiveAmbId, "Termo-higrômetro", cancellationToken);
            ambientThermometer = thermoHygrometer;
            hygrometer = thermoHygrometer;
        }
        else
        {
            ambientThermometer = await LoadAndValidateStandardAsync(effectiveAmbId, "Termômetro Ambiente", cancellationToken);
            hygrometer = await LoadAndValidateStandardAsync(effectiveHygId, "Higrômetro", cancellationToken);
        }

        TracLabs.Domain.Entities.StandardEquipment? pycnometer = null;
        ResolvedStandardCorrection? pycnometerResolved = null;

        if (request.WaterDensityGcm3.HasValue && request.WaterDensityGcm3.Value > 0)
        {
            if (!request.PycnometerStandardId.HasValue || request.PycnometerStandardId.Value == Guid.Empty)
                throw new ArgumentException("Ao informar a massa específica da água calculada por picnômetro, é obrigatório informar o padrão de picnômetro utilizado (com certificado válido).", nameof(request));

            pycnometer = await LoadAndValidateStandardAsync(request.PycnometerStandardId.Value, "Picnômetro", cancellationToken);
            pycnometerResolved = ResolveStandard(pycnometer, request.WaterDensityGcm3.Value, "Picnômetro", warnings);
        }
        else if (request.PycnometerStandardId.HasValue && request.PycnometerStandardId.Value != Guid.Empty)
        {
            throw new ArgumentException("O padrão de picnômetro foi informado, porém o valor da massa específica da água medida não foi fornecido.", nameof(request));
        }

        // ── 3. Determinação dos Pontos Operacionais Médios de Ensaio ─────────
        var defaultP = barometer.RangeMax > 0 && barometer.RangeMin > 0 ? (barometer.RangeMax + barometer.RangeMin) / 2m : 1013.25m;
        var defaultT = 20.0m;
        var defaultH = 50.0m;

        decimal liquidTempOperatingValue = request.Measurements.Average(m => m.WaterTemperatureCelsius);
        decimal ambientTempOperatingValue = request.Measurements.Average(m => m.AmbientTemperatureCelsius > 0 ? m.AmbientTemperatureCelsius : defaultT);
        decimal baroOperatingValue = request.Measurements.Average(m => m.AtmosphericPressureHpa > 0 ? m.AtmosphericPressureHpa : defaultP);
        decimal hygroOperatingValue = request.Measurements.Average(m => m.RelativeHumidityPercent > 0 ? m.RelativeHumidityPercent : defaultH);

        decimal meanEmptyMass = request.Measurements.Average(m => m.EmptyMassGrams);
        decimal meanFullMass = request.Measurements.Average(m => m.FullMassGrams);
        decimal meanNetMass = meanFullMass - meanEmptyMass;
        decimal balanceOperatingValue = meanNetMass > 0 ? meanNetMass : (request.NominalVolumeMl > 0 ? request.NominalVolumeMl : meanFullMass);

        // ── 4. Resolução Dinâmica das Curvas/Certificados dos Padrões ────────
        var balanceSnapshotResolved = ResolveStandard(balance, balanceOperatingValue, "Balança Analítica", warnings);
        ResolvedStandardCorrection balanceFullResolved;
        ResolvedStandardCorrection balanceEmptyResolved;

        if (meanEmptyMass > 0)
        {
            balanceFullResolved = ResolveStandard(balance, meanFullMass, "Balança Analítica (Massa Cheia)", warnings);
            balanceEmptyResolved = ResolveStandard(balance, meanEmptyMass, "Balança Analítica (Massa Vazia)", warnings);
        }
        else
        {
            balanceFullResolved = ResolveStandard(balance, balanceOperatingValue, "Balança Analítica", warnings);
            balanceEmptyResolved = new ResolvedStandardCorrection(0m, 0m, 0m, 2.0m, false, null, "TareZero");
        }

        var liquidThermometerResolved = ResolveStandard(liquidThermometer, liquidTempOperatingValue, "Termômetro do Líquido", warnings);
        var barometerResolved = ResolveStandard(barometer, baroOperatingValue, "Barômetro", warnings);
        var ambientThermometerResolved = ResolveStandard(ambientThermometer, ambientTempOperatingValue, "Termômetro Ambiente", warnings);
        var hygrometerResolved = ResolveStandard(hygrometer, hygroOperatingValue, "Higrômetro", warnings);

        // ── 5. Construção da Requisição de Cálculo Completa ──────────────────
        var calcReq = new VolumeCalibrationRequest
        {
            NominalVolumeMl = request.NominalVolumeMl,
            Material = request.Material,
            ThermalExpansionCoefficient = request.ThermalExpansionCoefficient,
            MeniscusSemiIntervalMl = request.MeniscusSemiIntervalMl,
            Measurements = request.Measurements,
            // Balança
            BalanceResolutionGrams = balance.Resolution > 0 ? balance.Resolution : 0.0001m,
            BalanceCertificateUncertaintyGrams = Math.Max(balanceFullResolved.CalculatedUncertainty, balanceEmptyResolved.CalculatedUncertainty),
            BalanceCoverageFactor = balanceFullResolved.CoverageFactor > 0 ? balanceFullResolved.CoverageFactor : 2.0m,
            BalanceFullCorrectionGrams = balanceFullResolved.CalculatedCorrection,
            BalanceEmptyCorrectionGrams = balanceEmptyResolved.CalculatedCorrection,
            // Termômetro Líquido
            WaterThermometerResolutionCelsius = liquidThermometer.Resolution > 0 ? liquidThermometer.Resolution : 0.01m,
            WaterThermometerUncertaintyCelsius = liquidThermometerResolved.CalculatedUncertainty,
            WaterThermometerCoverageFactor = liquidThermometerResolved.CoverageFactor > 0 ? liquidThermometerResolved.CoverageFactor : 2.0m,
            WaterThermometerCorrectionCelsius = liquidThermometerResolved.CalculatedCorrection,
            // Termômetro Ambiente
            AmbientThermometerResolutionCelsius = ambientThermometer.Resolution > 0 ? ambientThermometer.Resolution : 0.1m,
            AmbientThermometerUncertaintyCelsius = ambientThermometerResolved.CalculatedUncertainty,
            AmbientThermometerCoverageFactor = ambientThermometerResolved.CoverageFactor > 0 ? ambientThermometerResolved.CoverageFactor : 2.0m,
            AmbientThermometerCorrectionCelsius = ambientThermometerResolved.CalculatedCorrection,
            // Barômetro
            BarometerResolutionHpa = barometer.Resolution > 0 ? barometer.Resolution : 0.1m,
            BarometerUncertaintyHpa = barometerResolved.CalculatedUncertainty,
            BarometerCoverageFactor = barometerResolved.CoverageFactor > 0 ? barometerResolved.CoverageFactor : 2.0m,
            BarometerCorrectionHpa = barometerResolved.CalculatedCorrection,
            // Higrômetro
            HygrometerResolutionPercent = hygrometer.Resolution > 0 ? hygrometer.Resolution : 1.0m,
            HygrometerUncertaintyPercent = hygrometerResolved.CalculatedUncertainty,
            HygrometerCoverageFactor = hygrometerResolved.CoverageFactor > 0 ? hygrometerResolved.CoverageFactor : 2.0m,
            HygrometerCorrectionPercent = hygrometerResolved.CalculatedCorrection
        };

        if (request.WaterDensityGcm3.HasValue && request.WaterDensityGcm3.Value > 0)
        {
            calcReq.WaterDensityReferenceGramsPerCm3 = request.WaterDensityGcm3.Value;
            calcReq.WaterDensityUncertaintyGramsPerCm3 = request.WaterDensityUncertaintyGcm3
                ?? (pycnometerResolved?.CalculatedUncertainty > 0 ? pycnometerResolved.CalculatedUncertainty : 0.00005m);
            calcReq.WaterDensityCoverageFactor = pycnometerResolved?.CoverageFactor > 0 ? pycnometerResolved.CoverageFactor : 2.0m;
        }


        // ── 6. Fábrica de Snapshots dos Padrões (ISO 17025) ───────────────────
        List<CalibrationStandardUsed> BuildStandardsUsed(Guid calibrationRecordId)
        {
            var standardsUsed = new List<CalibrationStandardUsed>();

            var activeBalCert = balance.GetActiveCertificate();
            standardsUsed.Add(new(calibrationRecordId, balance, StandardRole.Balance, balanceSnapshotResolved, activeBalCert?.Id, activeBalCert?.CertificateNumber, activeBalCert?.CalibratingLaboratory, activeBalCert?.CalibrationDueDate ?? balance.CalibrationDueDate));

            var activeLiqCert = liquidThermometer.GetActiveCertificate();
            standardsUsed.Add(new(calibrationRecordId, liquidThermometer, StandardRole.LiquidThermometer, liquidThermometerResolved, activeLiqCert?.Id, activeLiqCert?.CertificateNumber, activeLiqCert?.CalibratingLaboratory, activeLiqCert?.CalibrationDueDate ?? liquidThermometer.CalibrationDueDate));

            var activeBarCert = barometer.GetActiveCertificate();
            standardsUsed.Add(new(calibrationRecordId, barometer, StandardRole.Barometer, barometerResolved, activeBarCert?.Id, activeBarCert?.CertificateNumber, activeBarCert?.CalibratingLaboratory, activeBarCert?.CalibrationDueDate ?? barometer.CalibrationDueDate));

            if (ambientThermometer.Id == hygrometer.Id)
            {
                var cert = ambientThermometer.GetActiveCertificate();
                standardsUsed.Add(new(calibrationRecordId, ambientThermometer, StandardRole.ThermoHygrometer, ambientThermometerResolved, cert?.Id, cert?.CertificateNumber, cert?.CalibratingLaboratory, cert?.CalibrationDueDate ?? ambientThermometer.CalibrationDueDate));
            }
            else
            {
                var activeAmbCert = ambientThermometer.GetActiveCertificate();
                standardsUsed.Add(new(calibrationRecordId, ambientThermometer, StandardRole.AmbientThermometer, ambientThermometerResolved, activeAmbCert?.Id, activeAmbCert?.CertificateNumber, activeAmbCert?.CalibratingLaboratory, activeAmbCert?.CalibrationDueDate ?? ambientThermometer.CalibrationDueDate));

                var activeHygCert = hygrometer.GetActiveCertificate();
                standardsUsed.Add(new(calibrationRecordId, hygrometer, StandardRole.Hygrometer, hygrometerResolved, activeHygCert?.Id, activeHygCert?.CertificateNumber, activeHygCert?.CalibratingLaboratory, activeHygCert?.CalibrationDueDate ?? hygrometer.CalibrationDueDate));
            }

            if (pycnometer != null && pycnometerResolved != null)
            {
                var activePycCert = pycnometer.GetActiveCertificate();
                standardsUsed.Add(new(calibrationRecordId, pycnometer, StandardRole.Pycnometer, pycnometerResolved, activePycCert?.Id, activePycCert?.CertificateNumber, activePycCert?.CalibratingLaboratory, activePycCert?.CalibrationDueDate ?? pycnometer.CalibrationDueDate));
            }

            return standardsUsed;
        }

        return new VolumeStandardsResolutionResult(
            calcReq,
            balance,
            liquidThermometer,
            barometer,
            ambientThermometer,
            hygrometer,
            balanceFullResolved,
            balanceEmptyResolved,
            liquidThermometerResolved,
            barometerResolved,
            ambientThermometerResolved,
            hygrometerResolved,
            warnings,
            BuildStandardsUsed
        );
    }

    private async Task<TracLabs.Domain.Entities.StandardEquipment> LoadAndValidateStandardAsync(
        Guid standardId, string roleName, CancellationToken cancellationToken)
    {
        var standard = await _standardEquipmentRepository.GetWithCertificatesAsync(standardId, cancellationToken)
                       ?? await _standardEquipmentRepository.GetByIdAsync(standardId, cancellationToken);

        if (standard == null)
            throw new KeyNotFoundException($"Padrão metrológico de {roleName} com ID '{standardId}' não encontrado.");

        if (!standard.IsUsableForCalibration)
        {
            throw new InvalidOperationException(
                $"O padrão '{standard.LabTag}' ({roleName}) não está apto para uso metrológico. Status: '{standard.Status}', Validade: '{standard.CalibrationDueDate:dd/MM/yyyy}'.");
        }

        return standard;
    }

    private static ResolvedStandardCorrection ResolveStandard(
        TracLabs.Domain.Entities.StandardEquipment standard,
        decimal operatingValue,
        string roleName,
        List<string> warnings)
    {
        var activeCert = standard.GetActiveCertificate();
        ResolvedStandardCorrection resolved;

        if (activeCert != null)
        {
            resolved = activeCert.ResolveCorrectionAt(operatingValue);
        }
        else
        {
            bool isExtrapolated = (standard.CalibrationRangeMin.HasValue && operatingValue < standard.CalibrationRangeMin.Value) ||
                                  (standard.CalibrationRangeMax.HasValue && operatingValue > standard.CalibrationRangeMax.Value);
            string? warn = isExtrapolated
                ? $"O valor operacional ({operatingValue:F4}) do padrão '{standard.LabTag}' ({roleName}) está fora da faixa calibrada ({standard.CalibrationRangeMin} a {standard.CalibrationRangeMax})."
                : null;

            resolved = new ResolvedStandardCorrection(
                operatingValue,
                standard.Correction,
                standard.ExpandedUncertainty,
                standard.CoverageFactor > 0 ? standard.CoverageFactor : 2.0m,
                isExtrapolated,
                warn,
                "StandardEquipmentDirect");
        }

        if (resolved.IsExtrapolated && !string.IsNullOrWhiteSpace(resolved.WarningMessage))
        {
            warnings.Add(resolved.WarningMessage);
        }

        return resolved;
    }
}
