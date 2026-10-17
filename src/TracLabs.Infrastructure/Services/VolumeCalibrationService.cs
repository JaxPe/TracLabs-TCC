using TracLabs.Application.DTOs.Calibration;
using TracLabs.Application.Interfaces;
using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;

namespace TracLabs.Infrastructure.Services;

/// <summary>
/// Motor de cálculo metrológico para calibração gravimétrica de instrumentos volumétricos de laboratório
/// (balões volumétricos, pipetas de um ou dois traços, pipetas graduadas, buretas e picnômetros),
/// em estrita conformidade com:
/// - ABNT NBR ISO 4787:2013 ("Vidraria de laboratório — Instrumentos volumétricos de vidro — Métodos de teste de capacidade e uso")
/// - DOC-CGCRE-027 ("Orientação para Acreditação de Laboratórios na Área de Medição de Volume")
/// - JCGM 100:2008 / Guia para a Expressão da Incerteza de Medição (GUM / INMETRO)
/// </summary>
public class VolumeCalibrationService : IVolumeCalibrationService
{
    // ── Constantes da Equação de Spieweck para Massa Específica do Ar ───────
    // Conforme ISO 4787 anexo A e DOC-CGCRE-027 equação da densidade do ar:
    // ra = [k1 * P - phi * (k2 * T - k3)] / (k4 + T)
    private const decimal K1 = 0.34844m;     // °C.kg / (m³.hPa)
    private const decimal K2 = 0.00252m;     // kg / m³
    private const decimal K3 = 0.020582m;    // °C.kg / m³
    private const decimal K4 = 273.15m;      // °C (ponto zero absoluto)

    // Temperatura de referência metrológica internacional para vidrarias de laboratório
    private const decimal ReferenceTemperatureCelsius = 20.0m;

    /// <summary>
    /// Executa o cálculo metrológico completo da calibração gravimétrica:
    /// 1. Determinação da densidade do ar (ra) e sua respectiva incerteza padrão u(ra)
    /// 2. Determinação da densidade da água (rw) compensada pela temperatura
    /// 3. Cálculo do volume individual corrigido a 20°C (V20,i) para cada repetição de pesagem
    /// 4. Média dos volumes, desvio padrão amostral e erro sistemático de indicação
    /// 5. Levantamento de todas as fontes de incerteza (balança, líquido, ar, dilatação, temperatura, repetibilidade e menisco)
    /// 6. Cálculo dos graus de liberdade efetivos pela fórmula de Welch-Satterthwaite
    /// 7. Cálculo do fator de abrangência (k) pela distribuição t de Student e da incerteza expandida U a 95.45% de confiança
    /// </summary>
    public VolumeCalibrationResultDto Calculate(VolumeCalibrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Measurements == null || request.Measurements.Count == 0)
            throw new ArgumentException("Pelo menos uma repetição de pesagem gravimétrica é obrigatória para a calibração.", nameof(request));

        if (request.Measurements.Count < 3)
            throw new ArgumentException("A ISO 4787 e o DOC-CGCRE-027 recomendam uma série mínima de pesagens repetidas para a estimativa da repetibilidade.", nameof(request));

        // ── 1. Coeficiente de Dilatação Térmica Volumétrica (gamma) ─────────────
        var gamma = request.ThermalExpansionCoefficient.HasValue && request.ThermalExpansionCoefficient.Value > 0
            ? request.ThermalExpansionCoefficient.Value
            : ResolveThermalExpansionCoefficient(request.Material);

        // ── 2. Correção das Condições Ambientais ────────────────────────────────
        var correctedPressureHpa = request.AtmosphericPressureHpa + request.BarometerCorrectionHpa;
        var correctedAmbientTemp = request.AmbientTemperatureCelsius + request.AmbientThermometerCorrectionCelsius;
        var correctedHumidity = request.RelativeHumidityPercent + request.HygrometerCorrectionPercent;

        // ── 3. Massa Específica do Ar (ra) e seu Balanço de Incerteza ───────────
        var airDensityKgPerM3 = CalculateAirDensity(correctedPressureHpa, correctedAmbientTemp, correctedHumidity);
        var airDensityGramsPerCm3 = airDensityKgPerM3 / 1000m; // Conversão de kg/m³ para g/cm³

        var airDensityStdUncertaintyKg = CalculateAirDensityStandardUncertainty(
            correctedPressureHpa, correctedAmbientTemp, correctedHumidity,
            airDensityKgPerM3, request);
        var airDensityStdUncertaintyGrams = airDensityStdUncertaintyKg / 1000m;

        // ── 4. Temperatura Média da Água e Densidade da Água (rw) ───────────────
        var meanWaterTempRaw = request.Measurements.Average(m => m.WaterTemperatureCelsius);
        var meanWaterTempCorrected = meanWaterTempRaw + request.WaterThermometerCorrectionCelsius;

        var waterDensity = request.WaterDensityReferenceGramsPerCm3.HasValue && request.WaterDensityReferenceGramsPerCm3.Value > 0
            ? request.WaterDensityReferenceGramsPerCm3.Value
            : CalculatePureWaterDensity(meanWaterTempCorrected);

        var weightsDensity = request.WeightsDensityGramsPerCm3 > 0 ? request.WeightsDensityGramsPerCm3 : 8.0m;

        // ── 5. Cálculo Individual dos Volumes V20 para Cada Medição ─────────────
        // Equação da ISO 4787 / DOC-CGCRE-027:
        // V20 = (Mc - Mv) * [1 / (rw - ra)] * [1 - (ra / rb)] * [1 - gamma * (T - 20)]
        var calculatedPoints = new List<VolumeGravimetricPoint>(request.Measurements.Count);

        // Conforme ABNT NBR ISO 4787:2013 e DOC-CGCRE-027, a tara do recipiente sob calibração
        // é obtida pela média das pesagens vazias do frasco seco e limpo
        var meanEmptyMass = request.Measurements.Average(m => m.EmptyMassGrams);
        var correctedMeanEmptyMass = meanEmptyMass + request.BalanceEmptyCorrectionGrams;

        foreach (var m in request.Measurements)
        {
            var correctedFullMass = m.FullMassGrams + request.BalanceFullCorrectionGrams;
            var apparentMass = correctedFullMass - correctedMeanEmptyMass;
            var pointWaterTemp = m.WaterTemperatureCelsius + request.WaterThermometerCorrectionCelsius;

            if (apparentMass <= 0)
                throw new InvalidOperationException($"Medição #{m.RunNumber}: A massa cheia ({m.FullMassGrams}g) deve ser superior à massa vazia média ({meanEmptyMass}g).");

            var buoyancyFactor = 1m / (waterDensity - airDensityGramsPerCm3);
            var balanceWeightsFactor = 1m - (airDensityGramsPerCm3 / weightsDensity);
            var expansionFactor = 1m - (gamma * (pointWaterTemp - ReferenceTemperatureCelsius));

            var v20 = apparentMass * buoyancyFactor * balanceWeightsFactor * expansionFactor;

            calculatedPoints.Add(new VolumeGravimetricPoint(
                m.RunNumber,
                m.FullMassGrams,
                m.EmptyMassGrams,
                m.WaterTemperatureCelsius,
                Math.Round(v20, 6)
            ));
        }

        // ── 6. Estatísticas Amostrais e Erro Sistemático ────────────────────────
        var n = calculatedPoints.Count;
        var volumes = calculatedPoints.Select(p => p.CalculatedVolumeMl).ToList();
        var meanV20 = volumes.Average();
        var systematicError = meanV20 - request.NominalVolumeMl;
        var correction = -systematicError;

        decimal sampleVariance = 0m;
        for (int i = 0; i < n; i++)
        {
            var diff = volumes[i] - meanV20;
            sampleVariance += diff * diff;
        }
        sampleVariance /= (n - 1);
        var sampleStdDev = (decimal)Math.Sqrt((double)sampleVariance);
        var repeatabilityUncertainty = sampleStdDev / (decimal)Math.Sqrt(n);

        // ── 7. Orçamento Completo de Incerteza Metrológica (GUM / DOC-CGCRE-027) ──
        var budget = new List<UncertaintyBudgetComponentDto>();

        // 7.1 Balança - Incerteza do Certificado na Massa Cheia (Mc)
        var uBalanceCert = request.BalanceCoverageFactor > 0
            ? request.BalanceCertificateUncertaintyGrams / request.BalanceCoverageFactor
            : 0m;
        // ci para Mc = 1 / rw (aprox 1.002)
        var ciMass = 1m / (waterDensity - airDensityGramsPerCm3);
        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Balança - Calibração Massa Cheia (Mc)",
            Symbol = "Mc",
            EstimatedValue = request.Measurements.Average(m => m.FullMassGrams),
            Unit = "g",
            StandardUncertainty = uBalanceCert,
            Distribution = "Normal",
            Divisor = request.BalanceCoverageFactor,
            SensitivityCoefficient = ciMass,
            UncertaintyContributionMl = Math.Abs(ciMass * uBalanceCert),
            DegreesOfFreedom = 1000m
        });

        // 7.2 Balança - Incerteza do Certificado na Massa Vazia (Mv)
        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Balança - Calibração Massa Vazia (Mv)",
            Symbol = "Mv",
            EstimatedValue = request.Measurements.Average(m => m.EmptyMassGrams),
            Unit = "g",
            StandardUncertainty = uBalanceCert,
            Distribution = "Normal",
            Divisor = request.BalanceCoverageFactor,
            SensitivityCoefficient = -ciMass,
            UncertaintyContributionMl = Math.Abs(ciMass * uBalanceCert),
            DegreesOfFreedom = 1000m
        });

        // 7.3 Balança - Resolução Digital (distribuição retangular a / sqrt(3))
        var uBalanceResolution = (request.BalanceResolutionGrams / 2m) / (decimal)Math.Sqrt(3);
        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Balança - Resolução da Leitura",
            Symbol = "Mresol",
            EstimatedValue = request.BalanceResolutionGrams,
            Unit = "g",
            StandardUncertainty = uBalanceResolution,
            Distribution = "Retangular",
            Divisor = (decimal)Math.Sqrt(3),
            SensitivityCoefficient = ciMass,
            UncertaintyContributionMl = Math.Abs(ciMass * uBalanceResolution),
            DegreesOfFreedom = 1000m
        });

        // 7.4 Massa Específica da Água (rw)
        var uWaterDensity = request.WaterDensityCoverageFactor > 0
            ? request.WaterDensityUncertaintyGramsPerCm3 / request.WaterDensityCoverageFactor
            : 0m;
        var ciWaterDensity = -meanV20 / (waterDensity - airDensityGramsPerCm3);
        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Massa Específica da Água Pura (rw)",
            Symbol = "rw",
            EstimatedValue = waterDensity,
            Unit = "g/cm³",
            StandardUncertainty = uWaterDensity,
            Distribution = "Normal",
            Divisor = request.WaterDensityCoverageFactor,
            SensitivityCoefficient = ciWaterDensity,
            UncertaintyContributionMl = Math.Abs(ciWaterDensity * uWaterDensity),
            DegreesOfFreedom = 1000m
        });

        // 7.5 Massa Específica do Ar (ra)
        var ciAirDensity = meanV20 * ((1m / (waterDensity - airDensityGramsPerCm3)) - (1m / (weightsDensity - airDensityGramsPerCm3)));
        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Massa Específica do Ar (ra)",
            Symbol = "ra",
            EstimatedValue = airDensityGramsPerCm3,
            Unit = "g/cm³",
            StandardUncertainty = airDensityStdUncertaintyGrams,
            Distribution = "Normal",
            Divisor = 2.0m,
            SensitivityCoefficient = ciAirDensity,
            UncertaintyContributionMl = Math.Abs(ciAirDensity * airDensityStdUncertaintyGrams),
            DegreesOfFreedom = 1000m
        });

        // 7.6 Massa Específica dos Pesos Padrão (rb)
        var uWeightsDensity = (0.1m) / (decimal)Math.Sqrt(3); // incerteza padrão retangular típica de 0.1 g/cm³
        var ciWeightsDensity = meanV20 * (airDensityGramsPerCm3 / (weightsDensity * weightsDensity));
        budget.Add(new UncertaintyBudgetComponentDto
        {
            SourceName = "Massa Específica dos Pesos da Balança (rb)",
            Symbol = "rb",
            EstimatedValue = weightsDensity,
            Unit = "g/cm³",
            StandardUncertainty = uWeightsDensity,
            Distribution = "Retangular",
            Divisor = (decimal)Math.Sqrt(3),
            SensitivityCoefficient = ciWeightsDensity,
            UncertaintyContributionMl = Math.Abs(ciWeightsDensity * uWeightsDensity),
            DegreesOfFreedom = 1000m
        });

        // 7.7 Coeficiente de Dilatação Térmica do Vidro (gamma)
        var uGamma = request.ThermalExpansionUncertainty / (decimal)Math.Sqrt(3);
        var ciGamma = -meanV20 * (meanWaterTempCorrected - ReferenceTemperatureCelsius);
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

        // 7.8 Temperatura do Líquido (Termômetro Padrão - Certificado)
        var uWaterTempCert = request.WaterThermometerCoverageFactor > 0
            ? request.WaterThermometerUncertaintyCelsius / request.WaterThermometerCoverageFactor
            : 0m;
        var ciWaterTemp = -meanV20 * gamma;
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

        // 7.9 Temperatura do Líquido - Resolução do Termômetro
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

        // 7.10 Repetibilidade das Pesagens (s / sqrt(n))
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

        // 7.11 Ajuste do Menisco (Incerteza visual de nivelamento do operador)
        // Conforme a planilha e a ISO 4787, se não especificado pelo usuário,
        // adota o semi-intervalo experimental da amplitude (Vmax - Vmin)/2 sob distribuição retangular.
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

        // ── 9. Graus de Liberdade Efetivos (Fórmula de Welch-Satterthwaite) ──────
        // veff = uc^4 / SUM( ui^4 / vi )
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
        // t-Student para 95.45% de confiança
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
            AirDensityKgPerM3 = Math.Round(airDensityKgPerM3, 6),
            AirDensityGramsPerCm3 = Math.Round(airDensityGramsPerCm3, 9),
            AirDensityStandardUncertaintyKgPerM3 = Math.Round(airDensityStdUncertaintyKg, 6),
            WaterDensityGramsPerCm3 = Math.Round(waterDensity, 9),
            RepeatabilityStandardDeviationMl = Math.Round(sampleStdDev, 6),
            RepeatabilityUncertaintyMl = Math.Round(repeatabilityUncertainty, 6),
            CombinedStandardUncertaintyMl = Math.Round(combinedUncertainty, 6),
            EffectiveDegreesOfFreedom = Math.Round(effectiveDegreesOfFreedom, 2),
            CoverageFactor = Math.Round(coverageFactor, 4),
            ExpandedUncertaintyMl = Math.Round(expandedUncertainty, 6),
            RelativeExpandedUncertaintyPercent = Math.Round(relativeExpandedUncertainty, 4),
            CalculatedPoints = calculatedPoints,
            UncertaintyBudget = budget
        };
    }

    // ── Métodos Auxiliares de Metrologia e Física ─────────────────────────────

    /// <summary>
    /// Calcula a massa específica do ar (kg/m³) através da equação simplificada de Spieweck
    /// recomendada pela ISO 4787 Anexo A e DOC-CGCRE-027.
    /// </summary>
    private static decimal CalculateAirDensity(decimal pressureHpa, decimal ambientTempC, decimal humidityPercent)
    {
        // ra = [k1 * P - phi * (k2 * T - k3)] / (k4 + T)
        var numerator = (K1 * pressureHpa) - (humidityPercent * ((K2 * ambientTempC) - K3));
        var denominator = K4 + ambientTempC;

        if (denominator <= 0)
            throw new InvalidOperationException("Temperatura ambiente inválida para cálculo da densidade do ar.");

        return numerator / denominator;
    }

    /// <summary>
    /// Calcula a incerteza padrão combinada da massa específica do ar u(ra) em kg/m³,
    /// aplicando derivadas parciais e incertezas dos instrumentos ambientais (barômetro, termômetro e higrômetro).
    /// </summary>
    private static decimal CalculateAirDensityStandardUncertainty(
        decimal pressureHpa, decimal ambientTempC, decimal humidityPercent,
        decimal airDensityKgPerM3, VolumeCalibrationRequest req)
    {
        var denominator = K4 + ambientTempC;

        // Derivadas parciais (coeficientes de sensibilidade)
        var cP = K1 / denominator;
        var cT = -((airDensityKgPerM3 + (humidityPercent * K2)) / denominator);
        var cPhi = -(((K2 * ambientTempC) - K3) / denominator);

        // Incertezas padrão dos instrumentos ambientais
        var uP = (req.BarometerCoverageFactor > 0 ? req.BarometerUncertaintyHpa / req.BarometerCoverageFactor : 0m);
        var uT = (req.AmbientThermometerCoverageFactor > 0 ? req.AmbientThermometerUncertaintyCelsius / req.AmbientThermometerCoverageFactor : 0m);
        var uPhi = (req.HygrometerCoverageFactor > 0 ? req.HygrometerUncertaintyPercent / req.HygrometerCoverageFactor : 0m);

        var variance = (cP * cP * uP * uP) + (cT * cT * uT * uT) + (cPhi * cPhi * uPhi * uPhi);
        return (decimal)Math.Sqrt((double)variance);
    }

    /// <summary>
    /// Calcula a massa específica da água pura desaerada (g/cm³) em função da temperatura (°C)
    /// segundo a formulação metrológica internacional de Tanaka et al. (ISO 4787 / PTB).
    /// </summary>
    private static decimal CalculatePureWaterDensity(decimal waterTemperatureCelsius)
    {
        var t = (double)waterTemperatureCelsius;

        // Parâmetros de Tanaka et al.
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
    /// Retorna o coeficiente padrão de dilatação térmica volumétrica (gamma em °C^-1) para o material especificado.
    /// Referência: ISO 4787 Tabela 1 e DOC-CGCRE-027.
    /// </summary>
    private static decimal ResolveThermalExpansionCoefficient(VolumetricMaterial material) => material switch
    {
        VolumetricMaterial.BorosilicateGlass33 => 9.9e-6m,
        VolumetricMaterial.BorosilicateGlass50 => 1.5e-5m,
        VolumetricMaterial.SodaLimeGlass      => 4.2e-5m, // Vidro soda-cal transparente comum da planilha
        VolumetricMaterial.Polypropylene       => 4.5e-4m,
        _                                      => 4.2e-5m
    };

    /// <summary>
    /// Calcula o fator de abrangência k correspondente a 95.45% de nível da confiança
    /// a partir dos graus de liberdade efetivos veff (distribuição t de Student).
    /// </summary>
    private static decimal CalculateStudentCoverageFactor(decimal effectiveDegreesOfFreedom)
    {
        if (effectiveDegreesOfFreedom >= 1000m)
            return 2.0000m;

        var v = (double)effectiveDegreesOfFreedom;
        if (v < 1.0)
            v = 1.0;

        // Aproximação clássica de alta precisão para t(0.0455/2, v) / nível 95.45%:
        // Conforme tabelas do GUM / NIST / Excel T.INV.2T(1 - 0.9545, v)
        if (v >= 100.0)
            return 2.0000m;

        // Expansão analítica de Cornish-Fisher / Hill para quantil bicaudal 95.45%
        var kApprox = 2.00 + (2.373 / v) + (2.81 / (v * v));
        return (decimal)Math.Min(kApprox, 3.50);
    }
}
