using TracLabs.Application.DTOs.Calibration;
using TracLabs.Application.Interfaces;
using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;

namespace TracLabs.Infrastructure.Services;

/// <summary>
/// Motor de cálculo metrológico para calibração gravimétrica de instrumentos volumétricos de laboratório
/// (balões volumétricos, pipetas, buretas, picnômetros e microvolumes), em estrita conformidade com:
/// - ABNT NBR ISO 4787:2013 ("Vidraria de laboratório — Métodos de teste de capacidade e uso")
/// - ABNT NBR ISO 8655-6 ("Aparelhos volumétricos acionados por pistão — Determinação gravimétrica")
/// - DOC-CGCRE-027 ("Orientação para Acreditação de Laboratórios na Área de Medição de Volume")
/// - JCGM 100:2008 / Guia para a Expressão da Incerteza de Medição (GUM / INMETRO)
/// </summary>
public class VolumeCalibrationService : IVolumeCalibrationService
{
    // ── Constantes da Equação de Spieweck para Massa Específica do Ar ───────
    // Conforme ISO 4787 anexo A, DOC-CGCRE-027 e planilhas analíticas de referência:
    // ra = [k1 * P - phi * (k2 * T - k3)] / (k4 + T)
    private const decimal K1 = 0.34844m;     // °C.kg / (m³.hPa)
    private const decimal K2 = 0.00252m;     // kg / m³
    private const decimal K3 = 0.020582m;    // °C.kg / m³
    private const decimal K4 = 273.15m;      // °C (zero absoluto)

    // Temperatura de referência metrológica internacional para vidrarias de laboratório
    private const decimal ReferenceTemperatureCelsius = 20.0m;

    /// <summary>
    /// Executa o cálculo metrológico completo da calibração gravimétrica:
    /// 1. Aferição do piso mínimo de repetições (mínimo de 7 para vidrarias gerais e 10 para microvolumes).
    /// 2. Determinação das condições ambientais médias e correções dos certificados.
    /// 3. Cálculo completo do balanço de incerteza da massa específica do ar (9 fontes de incerteza).
    /// 4. Cálculo individual da densidade do ar e do volume corrigido a 20°C (V20,i) para cada repetição.
    /// 5. Cálculo da densidade da água (Tanaka) corrigida pelo certificado do termômetro.
    /// 6. Levantamento de todas as fontes de incerteza do volume (balança, ar, água, dilatação, repetibilidade, menisco).
    /// 7. Cálculo dos graus de liberdade efetivos (Welch-Satterthwaite) e incerteza expandida U a 95.45% de confiança.
    /// </summary>
    public VolumeCalibrationResultDto Calculate(VolumeCalibrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Measurements == null || request.Measurements.Count == 0)
            throw new ArgumentException("Pelo menos uma repetição de pesagem gravimétrica é obrigatória para a calibração.", nameof(request));

        if (request.Measurements.Count < 7)
            throw new ArgumentException("A calibração volumétrica exige no mínimo 7 pontos de medição (repetições) para assegurar a confiabilidade estatística e graus de liberdade adequados conforme a ABNT NBR ISO 4787 e DOC-CGCRE-027.", nameof(request));

        var n = request.Measurements.Count;

        // ── 1. Coeficiente de Dilatação Térmica Volumétrica (gamma) ─────────────
        var gamma = request.ThermalExpansionCoefficient.HasValue && request.ThermalExpansionCoefficient.Value > 0
            ? request.ThermalExpansionCoefficient.Value
            : ResolveThermalExpansionCoefficient(request.Material);

        // ── 2. Médias e Desvios das Condições Ambientais ────────────────────────
        // Normalização das leituras ambientais por ponto com fallback seguro para os valores de cabeçalho
        var defaultP = request.AtmosphericPressureHpa > 0 ? request.AtmosphericPressureHpa : 1013.25m;
        var defaultT = request.AmbientTemperatureCelsius > 0 ? request.AmbientTemperatureCelsius : 20.0m;
        var defaultH = request.RelativeHumidityPercent > 0 ? request.RelativeHumidityPercent : 50.0m;

        var pList = request.Measurements
            .Select(m => m.AtmosphericPressureHpa > 0 ? m.AtmosphericPressureHpa : defaultP)
            .ToList();
        var tAmbList = request.Measurements
            .Select(m => m.AmbientTemperatureCelsius > 0 ? m.AmbientTemperatureCelsius : defaultT)
            .ToList();
        var hList = request.Measurements
            .Select(m => m.RelativeHumidityPercent > 0 ? m.RelativeHumidityPercent : defaultH)
            .ToList();

        var meanP = pList.Average();
        var meanTAmb = tAmbList.Average();
        var meanH = hList.Average();

        var sP = CalculateSampleStandardDeviation(pList);
        var sTAmb = CalculateSampleStandardDeviation(tAmbList);
        var sH = CalculateSampleStandardDeviation(hList);

        // Aplicação das correções dos certificados nos valores médios
        var correctedPressureHpa = meanP + request.BarometerCorrectionHpa;
        var correctedAmbientTempC = meanTAmb + request.AmbientThermometerCorrectionCelsius;
        var correctedHumidityPercent = meanH + request.HygrometerCorrectionPercent;

        // ── 3. Balanço de Incerteza da Massa Específica do Ar (ra) ──────────────
        // A densidade do ar atua em duas frentes fundamentais na calibração:
        // 1. Ponto a ponto (Seção 5): calcula-se rho_a,i em cada corrida para registrar a evolução temporal
        //    da sala climática e aplicar a correção de empuxo específica de cada repetição.
        // 2. Média operacional (aqui): a densidade média serve como ponto de operação para os coeficientes
        //    de sensibilidade analíticos (cP, cT, cH e ciAirDensity) do orçamento de incerteza do GUM.
        var airBudget = new List<UncertaintyBudgetComponentDto>();

        var denomAir = K4 + correctedAmbientTempC;
        if (denomAir <= 0)
            throw new InvalidOperationException("Temperatura ambiente inválida para cálculo da densidade do ar.");

        var meanAirDensityKgPerM3 = CalculateAirDensity(correctedPressureHpa, correctedAmbientTempC, correctedHumidityPercent);
        var meanAirDensityGramsPerCm3 = meanAirDensityKgPerM3 / 1000m;

        // Coeficientes de Sensibilidade do Ar
        var cP_air = K1 / denomAir;
        var cT_air = ((-K1 * correctedPressureHpa) - (K2 * K4 * correctedHumidityPercent) - (K3 * correctedHumidityPercent)) / (denomAir * denomAir);
        var cH_air = -((K2 * correctedAmbientTempC) - K3) / denomAir;

        // Incertezas padrão dos componentes do ar
        var uP_repe = n > 1 ? sP / (decimal)Math.Sqrt(n) : 0m;
        var uP_resol = (request.BarometerResolutionHpa / 2m) / (decimal)Math.Sqrt(3);
        var uP_cert = request.BarometerCoverageFactor > 0 ? request.BarometerUncertaintyHpa / request.BarometerCoverageFactor : 0m;

        var uT_repe = n > 1 ? sTAmb / (decimal)Math.Sqrt(n) : 0m;
        var uT_resol = (request.AmbientThermometerResolutionCelsius / 2m) / (decimal)Math.Sqrt(3);
        var uT_cert = request.AmbientThermometerCoverageFactor > 0 ? request.AmbientThermometerUncertaintyCelsius / request.AmbientThermometerCoverageFactor : 0m;

        var uH_repe = n > 1 ? sH / (decimal)Math.Sqrt(n) : 0m;
        var uH_resol = (request.HygrometerResolutionPercent / 2m) / (decimal)Math.Sqrt(3);
        var uH_cert = request.HygrometerCoverageFactor > 0 ? request.HygrometerUncertaintyPercent / request.HygrometerCoverageFactor : 0m;

        // 3.1 Prepe
        airBudget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Pressão Ambiente - Repetibilidade",
            Symbol = "Prepe",
            EstimatedValue = correctedPressureHpa,
            Unit = "hPa",
            StandardUncertainty = uP_repe,
            Distribution = "Normal",
            Divisor = (decimal)Math.Sqrt(n),
            SensitivityCoefficient = cP_air,
            UncertaintyContributionMl = Math.Abs(cP_air * uP_repe),
            DegreesOfFreedom = n - 1
        });

        // 3.2 Presol
        airBudget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Pressão Ambiente - Resolução do Barômetro",
            Symbol = "Presol",
            EstimatedValue = request.BarometerResolutionHpa,
            Unit = "hPa",
            StandardUncertainty = uP_resol,
            Distribution = "Retangular",
            Divisor = (decimal)Math.Sqrt(3),
            SensitivityCoefficient = cP_air,
            UncertaintyContributionMl = Math.Abs(cP_air * uP_resol),
            DegreesOfFreedom = 1000m
        });

        // 3.3 Pcert
        airBudget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Pressão Ambiente - Calibração do Barômetro",
            Symbol = "Pcert",
            EstimatedValue = request.BarometerCorrectionHpa,
            Unit = "hPa",
            StandardUncertainty = uP_cert,
            Distribution = "Normal",
            Divisor = request.BarometerCoverageFactor,
            SensitivityCoefficient = cP_air,
            UncertaintyContributionMl = Math.Abs(cP_air * uP_cert),
            DegreesOfFreedom = 1000m
        });

        // 3.4 Trepe
        airBudget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Temperatura Ambiente - Repetibilidade",
            Symbol = "Trepe",
            EstimatedValue = correctedAmbientTempC,
            Unit = "°C",
            StandardUncertainty = uT_repe,
            Distribution = "Normal",
            Divisor = (decimal)Math.Sqrt(n),
            SensitivityCoefficient = cT_air,
            UncertaintyContributionMl = Math.Abs(cT_air * uT_repe),
            DegreesOfFreedom = n - 1
        });

        // 3.5 Tresol
        airBudget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Temperatura Ambiente - Resolução do Termômetro",
            Symbol = "Tresol",
            EstimatedValue = request.AmbientThermometerResolutionCelsius,
            Unit = "°C",
            StandardUncertainty = uT_resol,
            Distribution = "Retangular",
            Divisor = (decimal)Math.Sqrt(3),
            SensitivityCoefficient = cT_air,
            UncertaintyContributionMl = Math.Abs(cT_air * uT_resol),
            DegreesOfFreedom = 1000m
        });

        // 3.6 Tcert
        airBudget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Temperatura Ambiente - Calibração do Termômetro",
            Symbol = "Tcert",
            EstimatedValue = request.AmbientThermometerCorrectionCelsius,
            Unit = "°C",
            StandardUncertainty = uT_cert,
            Distribution = "Normal",
            Divisor = request.AmbientThermometerCoverageFactor,
            SensitivityCoefficient = cT_air,
            UncertaintyContributionMl = Math.Abs(cT_air * uT_cert),
            DegreesOfFreedom = 1000m
        });

        // 3.7 Urepe
        airBudget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Umidade Relativa - Repetibilidade",
            Symbol = "Urepe",
            EstimatedValue = correctedHumidityPercent,
            Unit = "%",
            StandardUncertainty = uH_repe,
            Distribution = "Normal",
            Divisor = (decimal)Math.Sqrt(n),
            SensitivityCoefficient = cH_air,
            UncertaintyContributionMl = Math.Abs(cH_air * uH_repe),
            DegreesOfFreedom = n - 1
        });

        // 3.8 Uresol
        airBudget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Umidade Relativa - Resolução do Higrômetro",
            Symbol = "Uresol",
            EstimatedValue = request.HygrometerResolutionPercent,
            Unit = "%",
            StandardUncertainty = uH_resol,
            Distribution = "Retangular",
            Divisor = (decimal)Math.Sqrt(3),
            SensitivityCoefficient = cH_air,
            UncertaintyContributionMl = Math.Abs(cH_air * uH_resol),
            DegreesOfFreedom = 1000m
        });

        // 3.9 Ucert
        airBudget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Umidade Relativa - Calibração do Higrômetro",
            Symbol = "Ucert",
            EstimatedValue = request.HygrometerCorrectionPercent,
            Unit = "%",
            StandardUncertainty = uH_cert,
            Distribution = "Normal",
            Divisor = request.HygrometerCoverageFactor,
            SensitivityCoefficient = cH_air,
            UncertaintyContributionMl = Math.Abs(cH_air * uH_cert),
            DegreesOfFreedom = 1000m
        });

        // Incerteza combinada do ar uc(ra)
        decimal sumSquaredAir = 0m;
        foreach (var c in airBudget)
            sumSquaredAir += c.UncertaintyContributionMl * c.UncertaintyContributionMl;

        var ucAirKg = (decimal)Math.Sqrt((double)sumSquaredAir);
        var ucAirGramsPerCm3 = ucAirKg / 1000m;

        // Welch-Satterthwaite para o ar
        decimal sumWelchAir = 0m;
        foreach (var c in airBudget)
        {
            if (c.UncertaintyContributionMl > 0 && c.DegreesOfFreedom > 0)
            {
                var ui4 = Math.Pow((double)c.UncertaintyContributionMl, 4);
                sumWelchAir += (decimal)(ui4 / (double)c.DegreesOfFreedom);
            }
        }

        var veffAir = sumWelchAir > 0
            ? (decimal)(Math.Pow((double)ucAirKg, 4)) / sumWelchAir
            : 1000m;

        var kAir = CalculateStudentCoverageFactor(veffAir);
        var uExpandedAirKg = ucAirKg * kAir;

        // ── 4. Temperatura Média da Água e Densidade da Água (rw) ───────────────
        var meanWaterTempRaw = request.Measurements.Average(m => m.WaterTemperatureCelsius);
        var meanWaterTempCorrected = meanWaterTempRaw + request.WaterThermometerCorrectionCelsius;

        var waterDensity = request.WaterDensityReferenceGramsPerCm3.HasValue && request.WaterDensityReferenceGramsPerCm3.Value > 0
            ? request.WaterDensityReferenceGramsPerCm3.Value
            : CalculatePureWaterDensity(meanWaterTempCorrected);

        var weightsDensity = request.WeightsDensityGramsPerCm3 > 0 ? request.WeightsDensityGramsPerCm3 : 8.0m;

        // ── 5. Cálculo Individual dos Volumes V20 para Cada Medição ─────────────
        var calculatedPoints = new List<VolumeGravimetricPoint>(n);

        var meanEmptyMass = request.Measurements.Average(m => m.EmptyMassGrams);
        var correctedMeanEmptyMass = meanEmptyMass + request.BalanceEmptyCorrectionGrams;

        foreach (var m in request.Measurements)
        {
            var correctedFullMass = m.FullMassGrams + request.BalanceFullCorrectionGrams;
            var apparentMass = correctedFullMass - correctedMeanEmptyMass;
            var pointWaterTemp = m.WaterTemperatureCelsius + request.WaterThermometerCorrectionCelsius;

            if (apparentMass <= 0)
                throw new InvalidOperationException($"Medição #{m.RunNumber}: A massa cheia ({m.FullMassGrams}g) deve ser superior à massa vazia média ({meanEmptyMass}g).");

            // 5.1 Rastreamento pontual da densidade do ar (evolução temporal e empuxo específico)
            var pRaw = m.AtmosphericPressureHpa > 0 ? m.AtmosphericPressureHpa : defaultP;
            var tRaw = m.AmbientTemperatureCelsius > 0 ? m.AmbientTemperatureCelsius : defaultT;
            var hRaw = m.RelativeHumidityPercent > 0 ? m.RelativeHumidityPercent : defaultH;

            var pPoint = pRaw + request.BarometerCorrectionHpa;
            var tPoint = tRaw + request.AmbientThermometerCorrectionCelsius;
            var hPoint = hRaw + request.HygrometerCorrectionPercent;
            var pointAirDensityKg = CalculateAirDensity(pPoint, tPoint, hPoint);
            var pointAirDensityGrams = pointAirDensityKg / 1000m;

            var buoyancyFactor = 1m / (waterDensity - pointAirDensityGrams);
            var balanceWeightsFactor = 1m - (pointAirDensityGrams / weightsDensity);
            var expansionFactor = 1m - (gamma * (pointWaterTemp - ReferenceTemperatureCelsius));

            var v20 = apparentMass * buoyancyFactor * balanceWeightsFactor * expansionFactor;

            calculatedPoints.Add(new VolumeGravimetricPoint(
                m.RunNumber,
                m.FullMassGrams,
                m.EmptyMassGrams,
                m.WaterTemperatureCelsius,
                Math.Round(v20, 6),
                tRaw,
                pRaw,
                hRaw,
                Math.Round(pointAirDensityKg, 6)));
        }

        // ── 6. Estatísticas Finais dos Volumes ──────────────────────────────────
        var volumes = calculatedPoints.Select(p => p.CalculatedVolumeMl).ToList();
        var meanV20 = volumes.Average();
        var sampleStdDev = CalculateSampleStandardDeviation(volumes);
        var repeatabilityUncertainty = sampleStdDev / (decimal)Math.Sqrt(n);

        var systematicError = meanV20 - request.NominalVolumeMl;
        var correction = -systematicError;

        // ── 7. Balanço de Incerteza do Volume (V20) ─────────────────────────────
        var budget = new List<UncertaintyBudgetComponentDto>();

        var meanFullMass = request.Measurements.Average(m => m.FullMassGrams);
        var meanApparentMass = meanFullMass - meanEmptyMass;

        // Sensibilidade da Massa Cheia (Mc) e Massa Vazia (Mv)
        // cM = [1 / (rw - ra)] * [1 - (ra / rb)] * [1 - gamma * (Tliq - 20)]
        var ciMass = (1m / (waterDensity - meanAirDensityGramsPerCm3))
                     * (1m - (meanAirDensityGramsPerCm3 / weightsDensity))
                     * (1m - (gamma * (meanWaterTempCorrected - ReferenceTemperatureCelsius)));

        // 7.1 Balança - Calibração Massa Cheia (Mc)
        var uFullMassCert = request.BalanceCoverageFactor > 0 ? request.BalanceCertificateUncertaintyGrams / request.BalanceCoverageFactor : 0m;
        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Balança - Calibração Massa Cheia (Mc)",
            Symbol = "Mccert",
            EstimatedValue = meanFullMass + request.BalanceFullCorrectionGrams,
            Unit = "g",
            StandardUncertainty = uFullMassCert,
            Distribution = "Normal",
            Divisor = request.BalanceCoverageFactor,
            SensitivityCoefficient = ciMass,
            UncertaintyContributionMl = Math.Abs(ciMass * uFullMassCert),
            DegreesOfFreedom = 1000m
        });

        // 7.2 Balança - Calibração Massa Vazia (Mv)
        var uEmptyMassCert = request.BalanceCoverageFactor > 0 ? request.BalanceCertificateUncertaintyGrams / request.BalanceCoverageFactor : 0m;
        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Balança - Calibração Massa Vazia (Mv)",
            Symbol = "Mvcert",
            EstimatedValue = meanEmptyMass + request.BalanceEmptyCorrectionGrams,
            Unit = "g",
            StandardUncertainty = uEmptyMassCert,
            Distribution = "Normal",
            Divisor = request.BalanceCoverageFactor,
            SensitivityCoefficient = ciMass,
            UncertaintyContributionMl = Math.Abs(ciMass * uEmptyMassCert),
            DegreesOfFreedom = 1000m
        });

        // 7.3 Balança - Resolução da Leitura
        var uBalanceResol = (request.BalanceResolutionGrams / 2m) / (decimal)Math.Sqrt(3);
        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Balança - Resolução da Leitura",
            Symbol = "Mresol",
            EstimatedValue = request.BalanceResolutionGrams,
            Unit = "g",
            StandardUncertainty = uBalanceResol,
            Distribution = "Retangular",
            Divisor = (decimal)Math.Sqrt(3),
            SensitivityCoefficient = ciMass,
            UncertaintyContributionMl = Math.Abs(ciMass * uBalanceResol),
            DegreesOfFreedom = 1000m
        });

        // 7.4 Massa Específica da Água Pura (rw)
        var uWaterDensity = request.WaterDensityUncertaintyGramsPerCm3 > 0
            ? (request.WaterDensityCoverageFactor > 0 ? request.WaterDensityUncertaintyGramsPerCm3 / request.WaterDensityCoverageFactor : request.WaterDensityUncertaintyGramsPerCm3)
            : 0.0000282m; // Incerteza padrão típica Tanaka PTB (g/cm³)

        var ciWaterDensity = -(meanApparentMass / ((waterDensity - meanAirDensityGramsPerCm3) * (waterDensity - meanAirDensityGramsPerCm3)))
                             * (1m - (meanAirDensityGramsPerCm3 / weightsDensity))
                             * (1m - (gamma * (meanWaterTempCorrected - ReferenceTemperatureCelsius)));

        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Massa Específica da Água Pura (rw)",
            Symbol = "rw",
            EstimatedValue = waterDensity,
            Unit = "g/cm³",
            StandardUncertainty = uWaterDensity,
            Distribution = "Normal",
            Divisor = request.WaterDensityCoverageFactor > 0 ? request.WaterDensityCoverageFactor : 2.0m,
            SensitivityCoefficient = ciWaterDensity,
            UncertaintyContributionMl = Math.Abs(ciWaterDensity * uWaterDensity),
            DegreesOfFreedom = 21m
        });

        // 7.5 Massa Específica do Ar (ra) - Integração da incerteza combinada do ar
        var diffWaterAir = waterDensity - meanAirDensityGramsPerCm3;
        var ciAirDensity = ((meanApparentMass / (diffWaterAir * diffWaterAir)) * (1m - (meanAirDensityGramsPerCm3 / weightsDensity))
                           - ((1m / weightsDensity) * (meanApparentMass / diffWaterAir)))
                           * (1m - (gamma * (meanWaterTempCorrected - ReferenceTemperatureCelsius)));

        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Massa Específica do Ar (ra)",
            Symbol = "ra",
            EstimatedValue = meanAirDensityGramsPerCm3,
            Unit = "g/cm³",
            StandardUncertainty = ucAirGramsPerCm3,
            Distribution = "Normal",
            Divisor = 2.0m,
            SensitivityCoefficient = ciAirDensity,
            UncertaintyContributionMl = Math.Abs(ciAirDensity * ucAirGramsPerCm3),
            DegreesOfFreedom = veffAir
        });

        // 7.6 Massa Específica dos Pesos da Balança (rp)
        var uWeightsDensity = 0.1m / (decimal)Math.Sqrt(3);
        var ciWeightsDensity = (meanAirDensityGramsPerCm3 / (weightsDensity * weightsDensity))
                               * (meanApparentMass / diffWaterAir)
                               * (1m - (gamma * (meanWaterTempCorrected - ReferenceTemperatureCelsius)));

        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Massa Específica dos Pesos da Balança (rp)",
            Symbol = "rp",
            EstimatedValue = weightsDensity,
            Unit = "g/cm³",
            StandardUncertainty = uWeightsDensity,
            Distribution = "Retangular",
            Divisor = (decimal)Math.Sqrt(3),
            SensitivityCoefficient = ciWeightsDensity,
            UncertaintyContributionMl = Math.Abs(ciWeightsDensity * uWeightsDensity),
            DegreesOfFreedom = 1000m
        });

        // 7.7 Coeficiente de Dilatação Térmica do Vidro/Plástico (gamma)
        var uGamma = (request.ThermalExpansionUncertainty > 0 ? request.ThermalExpansionUncertainty : 8.66e-6m) / (decimal)Math.Sqrt(3);
        var ciGamma = -(meanApparentMass / diffWaterAir)
                      * (1m - (meanAirDensityGramsPerCm3 / weightsDensity))
                      * (meanWaterTempCorrected - ReferenceTemperatureCelsius);

        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Coeficiente de Dilatação Térmica do Recipiente (gamma)",
            Symbol = "gamma",
            EstimatedValue = gamma,
            Unit = "°C^-1",
            StandardUncertainty = uGamma,
            Distribution = "Retangular",
            Divisor = (decimal)Math.Sqrt(3),
            SensitivityCoefficient = ciGamma,
            UncertaintyContributionMl = Math.Abs(ciGamma * uGamma),
            DegreesOfFreedom = 1000m
        });

        // 7.8 Temperatura da Água - Calibração do Termômetro
        // cTw = dV20 / dTw = - [m / (rw - ra)^2] * [1 - ra/rb] * [1 - gamma*(Tw - 20)] * (d_rw/d_Tw)
        //                   - [m / (rw - ra)] * [1 - ra/rb] * gamma
        var dWaterDensityDt = CalculatePureWaterDensityDerivative(meanWaterTempCorrected);

        var ciWaterTempDensityTerm = -(meanApparentMass / (diffWaterAir * diffWaterAir))
                                     * (1m - (meanAirDensityGramsPerCm3 / weightsDensity))
                                     * (1m - (gamma * (meanWaterTempCorrected - ReferenceTemperatureCelsius)))
                                     * dWaterDensityDt;

        var ciWaterTempExpansionTerm = -(meanApparentMass / diffWaterAir)
                                       * (1m - (meanAirDensityGramsPerCm3 / weightsDensity))
                                       * gamma;

        var ciWaterTemp = ciWaterTempDensityTerm + ciWaterTempExpansionTerm;

        var uWaterTempCert = request.WaterThermometerCoverageFactor > 0
            ? request.WaterThermometerUncertaintyCelsius / request.WaterThermometerCoverageFactor
            : 0m;

        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Temperatura da Água - Calibração do Termômetro",
            Symbol = "Tcert",
            EstimatedValue = meanWaterTempCorrected,
            Unit = "°C",
            StandardUncertainty = uWaterTempCert,
            Distribution = "Normal",
            Divisor = request.WaterThermometerCoverageFactor,
            SensitivityCoefficient = ciWaterTemp,
            UncertaintyContributionMl = Math.Abs(ciWaterTemp * uWaterTempCert),
            DegreesOfFreedom = 1000m
        });

        // 7.9 Temperatura da Água - Resolução do Termômetro
        var uWaterTempResol = (request.WaterThermometerResolutionCelsius / 2m) / (decimal)Math.Sqrt(3);
        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Temperatura da Água - Resolução do Termômetro",
            Symbol = "Tresol",
            EstimatedValue = request.WaterThermometerResolutionCelsius,
            Unit = "°C",
            StandardUncertainty = uWaterTempResol,
            Distribution = "Retangular",
            Divisor = (decimal)Math.Sqrt(3),
            SensitivityCoefficient = ciWaterTemp,
            UncertaintyContributionMl = Math.Abs(ciWaterTemp * uWaterTempResol),
            DegreesOfFreedom = 1000m
        });

        // 7.10 Repetibilidade Experimental (s / sqrt(n))
        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Repetibilidade Experimental das Leituras",
            Symbol = "Repe",
            EstimatedValue = sampleStdDev,
            Unit = "mL",
            StandardUncertainty = repeatabilityUncertainty,
            Distribution = "Normal",
            Divisor = (decimal)Math.Sqrt(n),
            SensitivityCoefficient = 1.0m,
            UncertaintyContributionMl = repeatabilityUncertainty,
            DegreesOfFreedom = n - 1
        });

        // 7.11 Ajuste do Menisco (Operador)
        var meniscusSemiInterval = request.MeniscusSemiIntervalMl.HasValue && request.MeniscusSemiIntervalMl.Value > 0
            ? request.MeniscusSemiIntervalMl.Value
            : (volumes.Max() - volumes.Min()) / 2m;

        var uMeniscus = meniscusSemiInterval / (decimal)Math.Sqrt(3);
        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Ajuste Visual do Menisco (Operador)",
            Symbol = "Menisco",
            EstimatedValue = meniscusSemiInterval,
            Unit = "mL",
            StandardUncertainty = uMeniscus,
            Distribution = "Retangular",
            Divisor = (decimal)Math.Sqrt(3),
            SensitivityCoefficient = 1.0m,
            UncertaintyContributionMl = uMeniscus,
            DegreesOfFreedom = 1000m
        });

        // ── 8. Incerteza Combinada uc(V) ────────────────────────────────────────
        decimal sumSquaredContributions = 0m;
        foreach (var c in budget)
        {
            sumSquaredContributions += c.UncertaintyContributionMl * c.UncertaintyContributionMl;
        }
        var combinedUncertainty = (decimal)Math.Sqrt((double)sumSquaredContributions);

        // ── 9. Graus de Liberdade Efetivos do Volume (Welch-Satterthwaite) ──────
        decimal sumWelchTerms = 0m;
        foreach (var c in budget)
        {
            if (c.UncertaintyContributionMl > 0 && c.DegreesOfFreedom > 0)
            {
                var ui4 = Math.Pow((double)c.UncertaintyContributionMl, 4);
                sumWelchTerms += (decimal)(ui4 / (double)c.DegreesOfFreedom);
            }
        }

        var effectiveDegreesOfFreedom = sumWelchTerms > 0
            ? (decimal)(Math.Pow((double)combinedUncertainty, 4)) / sumWelchTerms
            : 1000m;

        // ── 10. Fator de Abrangência (k) e Incerteza Expandida (U) ───────────────
        var coverageFactor = CalculateStudentCoverageFactor(effectiveDegreesOfFreedom);
        var expandedUncertainty = combinedUncertainty * coverageFactor;
        var relativeExpandedUncertainty = meanV20 > 0
            ? (expandedUncertainty / meanV20) * 100m
            : 0m;

        return new VolumeCalibrationResultDto
        {
            NominalVolumeMl = request.NominalVolumeMl,
            PointsCount = n,
            MeanVolumeV20Ml = Math.Round(meanV20, 6),
            SystematicErrorMl = Math.Round(systematicError, 6),
            CorrectionMl = Math.Round(correction, 6),
            AirDensityKgPerM3 = Math.Round(meanAirDensityKgPerM3, 6),
            AirDensityGramsPerCm3 = Math.Round(meanAirDensityGramsPerCm3, 9),
            AirDensityStandardUncertaintyKgPerM3 = Math.Round(ucAirKg, 6),
            AirDensityExpandedUncertaintyKgPerM3 = Math.Round(uExpandedAirKg, 6),
            AirDensityCoverageFactor = Math.Round(kAir, 4),
            AirDensityEffectiveDegreesOfFreedom = Math.Round(veffAir, 2),
            WaterDensityGramsPerCm3 = Math.Round(waterDensity, 9),
            RepeatabilityStandardDeviationMl = Math.Round(sampleStdDev, 6),
            RepeatabilityUncertaintyMl = Math.Round(repeatabilityUncertainty, 6),
            CombinedStandardUncertaintyMl = Math.Round(combinedUncertainty, 6),
            EffectiveDegreesOfFreedom = Math.Round(effectiveDegreesOfFreedom, 2),
            CoverageFactor = Math.Round(coverageFactor, 4),
            ExpandedUncertaintyMl = Math.Round(expandedUncertainty, 6),
            RelativeExpandedUncertaintyPercent = Math.Round(relativeExpandedUncertainty, 4),
            CalculatedPoints = calculatedPoints,
            AirDensityUncertaintyBudget = airBudget,
            UncertaintyBudget = budget
        };
    }

    // ── Métodos Auxiliares de Metrologia e Estatística ─────────────────────────

    private static decimal CalculateAirDensity(decimal pressureHpa, decimal ambientTempC, decimal humidityPercent)
    {
        var numerator = (K1 * pressureHpa) - (humidityPercent * ((K2 * ambientTempC) - K3));
        var denominator = K4 + ambientTempC;

        if (denominator <= 0)
            throw new InvalidOperationException("Temperatura ambiente inválida para cálculo da densidade do ar.");

        return numerator / denominator;
    }

    private static decimal CalculatePureWaterDensity(decimal waterTemperatureCelsius)
    {
        var t = (double)waterTemperatureCelsius;

        // Formulação internacional Tanaka et al. (ISO 4787 / PTB)
        const double a1 = -3.983035;
        const double a2 = 301.797;
        const double a3 = 522528.9;
        const double a4 = 69.34881;
        const double a5 = 0.999974950; // g/cm³ a 3.98°C

        var term = (Math.Pow(t + a1, 2) * (t + a2)) / (a3 * (t + a4));
        var density = a5 * (1.0 - term);

        return (decimal)density;
    }

    /// <summary>
    /// Derivada analítica exata da equação de Tanaka d(rho_w)/dt em g/(cm³.°C)
    /// utilizada para cálculo do coeficiente de sensibilidade do termômetro de líquido.
    /// </summary>
    private static decimal CalculatePureWaterDensityDerivative(decimal waterTemperatureCelsius)
    {
        var t = (double)waterTemperatureCelsius;

        const double a1 = -3.983035;
        const double a2 = 301.797;
        const double a3 = 522528.9;
        const double a4 = 69.34881;
        const double a5 = 0.999974950;

        var tPlusA1 = t + a1;
        var tPlusA2 = t + a2;
        var tPlusA4 = t + a4;

        var u = Math.Pow(tPlusA1, 2) * tPlusA2;
        var uPrime = tPlusA1 * (3.0 * t + 2.0 * a2 + a1);

        var fPrime = (uPrime * tPlusA4 - u) / (a3 * Math.Pow(tPlusA4, 2));
        var dRhoDt = -a5 * fPrime;

        return (decimal)dRhoDt;
    }

    private static decimal CalculateSampleStandardDeviation(IList<decimal> values)
    {
        if (values == null || values.Count <= 1)
            return 0m;

        var avg = values.Average();
        double sumSq = 0;
        foreach (var v in values)
        {
            var diff = (double)(v - avg);
            sumSq += diff * diff;
        }

        return (decimal)Math.Sqrt(sumSq / (values.Count - 1));
    }

    private static decimal ResolveThermalExpansionCoefficient(VolumetricMaterial material) => material switch
    {
        VolumetricMaterial.BorosilicateGlass33 => 9.9e-6m,
        VolumetricMaterial.BorosilicateGlass50 => 1.5e-5m,
        VolumetricMaterial.SodaLimeGlass      => 4.2e-5m,
        VolumetricMaterial.Polypropylene       => 4.5e-4m,
        _                                      => 4.2e-5m
    };

    private static decimal CalculateStudentCoverageFactor(decimal effectiveDegreesOfFreedom)
    {
        if (effectiveDegreesOfFreedom >= 1000m)
            return 2.0000m;

        var v = (double)effectiveDegreesOfFreedom;
        if (v < 1.0)
            v = 1.0;

        if (v >= 100.0)
            return 2.0000m;

        // Aproximação clássica de alta fidelidade para o quantil bicaudal 95.45% (t-Student)
        // Bate com TINV(1 - 0.9545, v) do Excel
        var kApprox = 2.00 + (2.373 / v) + (2.81 / (v * v));
        return (decimal)Math.Min(kApprox, 3.50);
    }
}
