using TracLabs.Frontend.Models;
using TracLabs.Frontend.Models.Enums;

namespace TracLabs.Frontend.Services;

public static class MetrologyCalculations
{
    private const decimal K1 = 0.34844m;
    private const decimal K2 = 0.00252m;
    private const decimal K3 = 0.020582m;
    private const decimal K4 = 273.15m;
    private const decimal ReferenceTemperatureCelsius = 20.0m;
    private const decimal ReferenceWeightsDensityGramsPerCm3 = 8.0m;

    /// <summary>
    /// Calcula a densidade da água pura em g/cm³ pela formulação de Tanaka et al. (ISO 4787 / ISO 8655-6)
    /// </summary>
    public static decimal CalculatePureWaterDensity(decimal waterTemperatureCelsius)
    {
        var t = (double)waterTemperatureCelsius;
        const double a1 = -3.983035;
        const double a2 = 301.797;
        const double a3 = 522528.9;
        const double a4 = 69.34881;
        const double a5 = 0.999974950;

        var term = (Math.Pow(t + a1, 2) * (t + a2)) / (a3 * (t + a4));
        var density = a5 * (1.0 - term);
        return (decimal)density;
    }

    /// <summary>
    /// Calcula a densidade do ar em kg/m³ pela equação de Spieweck (ISO 4787 / DOC-CGCRE-027)
    /// </summary>
    public static decimal CalculateAirDensity(decimal pressureHpa, decimal ambientTempC, decimal humidityPercent)
    {
        var num = (K1 * pressureHpa) - (humidityPercent * ((K2 * ambientTempC) - K3));
        var den = K4 + ambientTempC;
        if (den <= 0) return 1.200m;
        return num / den;
    }

    /// <summary>
    /// Retorna o coeficiente de dilatação volumétrica cúbica do material (gamma em °C^-1)
    /// </summary>
    public static decimal ResolveThermalExpansionCoefficient(VolumetricMaterial material) => material switch
    {
        VolumetricMaterial.BorosilicateGlass33 => 9.9e-6m,
        VolumetricMaterial.BorosilicateGlass50 => 1.5e-5m,
        VolumetricMaterial.SodaLimeGlass      => 4.2e-5m,
        VolumetricMaterial.Polypropylene       => 4.5e-4m,
        _                                      => 4.2e-5m
    };

    /// <summary>
    /// Determina o Erro Máximo Admissível (EMA / Tolerância) normativo conforme ISO 1042 / ABNT NBR ISO 1042
    /// para balões volumétricos (Classe A ou Classe B).
    /// </summary>
    public static decimal ResolveMaxPermissibleError(decimal nominalCapacity, string? accuracyClass = null)
    {
        var isClassB = accuracyClass?.Contains("B", StringComparison.OrdinalIgnoreCase) == true;
        var factor = isClassB ? 2.0m : 1.0m;

        decimal emaClassA = nominalCapacity switch
        {
            <= 1m => 0.010m,
            <= 2m => 0.015m,
            <= 5m => 0.025m,
            <= 10m => 0.025m,
            <= 20m => 0.040m,
            <= 25m => 0.040m,
            <= 50m => 0.060m,
            <= 100m => 0.100m,
            <= 200m => 0.150m,
            <= 250m => 0.150m,
            <= 500m => 0.250m,
            <= 1000m => 0.400m,
            <= 2000m => 0.600m,
            <= 5000m => 1.200m,
            _ => Math.Round(nominalCapacity * 0.001m, 3)
        };

        return emaClassA * factor;
    }

    /// <summary>
    /// Calcula o fator gravimétrico Z em mL/g
    /// Z = [1 / (rho_w - rho_a)] * [1 - (rho_a / rho_b)] * [1 - gamma * (t_w - 20)]
    /// </summary>
    public static decimal CalculateZFactor(
        decimal waterDensityGramsPerCm3,
        decimal airDensityKgPerM3,
        VolumetricMaterial material,
        decimal waterTempC,
        decimal weightsDensityGramsPerCm3 = ReferenceWeightsDensityGramsPerCm3)
    {
        var airDensityGramsPerCm3 = airDensityKgPerM3 / 1000m;
        var gamma = ResolveThermalExpansionCoefficient(material);

        var buoyancyFactor = 1m / (waterDensityGramsPerCm3 - airDensityGramsPerCm3);
        var balanceWeightsFactor = 1m - (airDensityGramsPerCm3 / weightsDensityGramsPerCm3);
        var expansionFactor = 1m - (gamma * (waterTempC - ReferenceTemperatureCelsius));

        return buoyancyFactor * balanceWeightsFactor * expansionFactor;
    }

    /// <summary>
    /// Converte a massa aparente para volume a 20°C (V20 em mL)
    /// </summary>
    public static decimal CalculateV20(decimal apparentMassGrams, decimal zFactor)
    {
        return apparentMassGrams * zFactor;
    }

    /// <summary>
    /// Desvio padrão amostral s(V)
    /// </summary>
    public static decimal CalculateSampleStandardDeviation(IList<decimal> values)
    {
        if (values == null || values.Count <= 1) return 0m;
        var avg = values.Average();
        double sumSq = 0;
        foreach (var v in values)
        {
            var diff = (double)(v - avg);
            sumSq += diff * diff;
        }
        return (decimal)Math.Sqrt(sumSq / (values.Count - 1));
    }

    /// <summary>
    /// Fator de abrangência k de Student para ~95.45% de confiança
    /// </summary>
    public static decimal CalculateCoverageFactor(int n)
    {
        if (n <= 1) return 2.00m;
        var v = n - 1;
        if (v >= 100) return 2.00m;
        var kApprox = 2.00 + (2.373 / v) + (2.81 / (v * v));
        return Math.Round((decimal)Math.Min(kApprox, 3.50), 2);
    }

    /// <summary>
    /// Calcula a incerteza combinada uc considerando repetibilidade, balança, temperatura e menisco
    /// </summary>
    public static decimal CalculateCombinedUncertainty(decimal sampleStdDev, int n)
    {
        if (n <= 0) return 0m;
        var uRepe = (double)(sampleStdDev / (decimal)Math.Sqrt(n));
        const double uBalance = 0.00020; // Incerteza padrão da balança
        const double uTemp = 0.00200;    // Incerteza padrão da temperatura da água
        const double uMeniscus = 0.00250;// Incerteza de ajuste do menisco

        var ucSq = (uRepe * uRepe) + (uBalance * uBalance) + (uTemp * uTemp) + (uMeniscus * uMeniscus);
        return (decimal)Math.Sqrt(ucSq);
    }

    /// <summary>
    /// Realiza o cálculo metrológico completo a partir das repetições e condições ambientais por ponto,
    /// suportando densidade manual da água (picnômetro) ou formulação analítica Tanaka (água pura).
    /// </summary>
    public static void ComputeCalibrationResults(
        CalibrationRecordDto record,
        decimal nominalCapacity,
        decimal maxPermissibleError)
    {
        var validPoints = record.Measurements
            .Where(m => m.FullMassGrams > m.EmptyMassGrams)
            .ToList();

        if (validPoints.Count == 0) return;

        var effectiveNominal = nominalCapacity > 0 ? nominalCapacity : (record.NominalCapacity > 0 ? record.NominalCapacity : 100.0m);
        var effectiveEma = maxPermissibleError > 0 ? maxPermissibleError : (record.MaxPermissibleError > 0 ? record.MaxPermissibleError : ResolveMaxPermissibleError(effectiveNominal, record.AccuracyClass));
        record.NominalCapacity = effectiveNominal;
        record.MaxPermissibleError = effectiveEma;

        // 1. Médias aritméticas das condições ambientais dos pontos válidos de medição
        var pointsWithWaterTemp = validPoints.Where(p => p.WaterTemperatureCelsius > 0).ToList();
        var pointsWithAmbTemp = validPoints.Where(p => p.AmbientTemperatureCelsius > 0).ToList();
        var pointsWithPressure = validPoints.Where(p => p.AtmosphericPressureHpa > 0).ToList();
        var pointsWithHumidity = validPoints.Where(p => p.RelativeHumidityPercent > 0).ToList();

        var meanTWater = pointsWithWaterTemp.Count > 0 
            ? pointsWithWaterTemp.Average(p => p.WaterTemperatureCelsius) 
            : (record.WaterTemperatureCelsius > 0 ? record.WaterTemperatureCelsius : 20.0m);

        var meanTAmb = pointsWithAmbTemp.Count > 0 
            ? pointsWithAmbTemp.Average(p => p.AmbientTemperatureCelsius) 
            : (record.AmbientTemperatureCelsius > 0 ? record.AmbientTemperatureCelsius : 20.0m);

        var meanP = pointsWithPressure.Count > 0 
            ? pointsWithPressure.Average(p => p.AtmosphericPressureHpa) 
            : (record.AtmosphericPressureHpa > 0 ? record.AtmosphericPressureHpa : 1013.25m);

        var meanH = pointsWithHumidity.Count > 0 
            ? pointsWithHumidity.Average(p => p.RelativeHumidityPercent) 
            : (record.RelativeHumidityPercent > 0 ? record.RelativeHumidityPercent : 50.0m);

        // Atualiza as propriedades consolidadas do registro com a média aritmética dos pontos
        record.WaterTemperatureCelsius = Math.Round(meanTWater, 2);
        record.AmbientTemperatureCelsius = Math.Round(meanTAmb, 2);
        record.AtmosphericPressureHpa = Math.Round(meanP, 1);
        record.RelativeHumidityPercent = Math.Round(meanH, 1);

        // 2. Determinação da densidade da água para o ensaio:
        // Caso WaterDensityGcm3 seja informada manualmente (> 0), utiliza-a diretamente (modo picnômetro).
        // Caso contrário, calcula automaticamente via Tanaka utilizando a média das temperaturas da água dos pontos.
        var useManualWaterDensity = record.WaterDensityGcm3.HasValue && record.WaterDensityGcm3.Value > 0;
        var baseWaterDensity = useManualWaterDensity
            ? record.WaterDensityGcm3!.Value
            : CalculatePureWaterDensity(record.WaterTemperatureCelsius);

        record.CalculatedWaterDensity = baseWaterDensity;
        record.CalculatedAirDensity = CalculateAirDensity(meanP, meanTAmb, meanH);
        record.CalculatedZFactor = CalculateZFactor(
            record.CalculatedWaterDensity, 
            record.CalculatedAirDensity, 
            record.Material, 
            record.WaterTemperatureCelsius);

        var v20List = new List<decimal>();
        foreach (var pt in validPoints)
        {
            // 3. Condições ambientais por ponto com fallback seguro para as médias calculadas
            var pPoint = pt.AtmosphericPressureHpa > 0 ? pt.AtmosphericPressureHpa : meanP;
            var tAmbPoint = pt.AmbientTemperatureCelsius > 0 ? pt.AmbientTemperatureCelsius : meanTAmb;
            var hPoint = pt.RelativeHumidityPercent > 0 ? pt.RelativeHumidityPercent : meanH;
            var tWaterPoint = pt.WaterTemperatureCelsius > 0 ? pt.WaterTemperatureCelsius : meanTWater;

            // 4. Densidade do ar pontual (Spieweck)
            var pointAirDensityKg = CalculateAirDensity(pPoint, tAmbPoint, hPoint);
            pt.CalculatedAirDensity = Math.Round(pointAirDensityKg, 4);

            // 5. Densidade da água para o ponto (Manual ou Tanaka na temperatura pontual)
            var pointWaterDensity = useManualWaterDensity
                ? record.WaterDensityGcm3!.Value
                : CalculatePureWaterDensity(tWaterPoint);

            // 6. Fator Z pontual e Volume corrigido a 20°C
            var zPoint = CalculateZFactor(
                pointWaterDensity,
                pointAirDensityKg,
                record.Material,
                tWaterPoint);

            pt.CalculatedZFactor = Math.Round(zPoint, 5);
            pt.CalculatedVolumeMl = Math.Round(CalculateV20(pt.ApparentWaterMassGrams, zPoint), 4);
            pt.DeviationMl = Math.Round(pt.CalculatedVolumeMl - effectiveNominal, 4);
            v20List.Add(pt.CalculatedVolumeMl);
        }

        record.MeanV20 = Math.Round(v20List.Average(), 4);
        record.SampleStdDev = Math.Round(CalculateSampleStandardDeviation(v20List), 5);
        record.RepeatabilityUncertainty = Math.Round(record.SampleStdDev / (decimal)Math.Sqrt(v20List.Count), 5);
        record.SystematicError = Math.Round(record.MeanV20 - effectiveNominal, 4);
        record.RelativeSystematicErrorPercent = effectiveNominal > 0 
            ? Math.Round((record.SystematicError / effectiveNominal) * 100m, 4) 
            : 0m;
        record.CoefficientOfVariationPercent = record.MeanV20 > 0 
            ? Math.Round((record.SampleStdDev / record.MeanV20) * 100m, 4) 
            : 0m;

        record.CombinedUncertainty = Math.Round(CalculateCombinedUncertainty(record.SampleStdDev, v20List.Count), 5);
        record.CoverageFactor = CalculateCoverageFactor(v20List.Count);
        record.ExpandedUncertainty = Math.Round(record.CombinedUncertainty * record.CoverageFactor, 4);

        if (Math.Abs(record.SystematicError) <= effectiveEma)
        {
            record.Result = CalibrationResult.Conforming;
            record.ConformityStatement = $"O instrumento atende aos critérios de aceitação da classe ({record.AccuracyClass}). O erro sistemático ({record.SystematicError:F4} mL) é inferior ao erro máximo admissível (±{effectiveEma:F3} mL).";
        }
        else
        {
            record.Result = CalibrationResult.NonConforming;
            record.ConformityStatement = $"O instrumento NÃO atende aos critérios de aceitação da classe ({record.AccuracyClass}). O erro sistemático ({record.SystematicError:F4} mL) excede o erro máximo admissível (±{effectiveEma:F3} mL).";
        }
    }

    /// <summary>
    /// Conjunto de 11 repetições de pesagem retirado diretamente da planilha real 100 mL (planilha volume 11-08-25.xlsx)
    /// </summary>
    public static List<VolumeMeasurementPointDto> GetBenchmark100MlPoints()
    {
        return new List<VolumeMeasurementPointDto>
        {
            new() { RunNumber = 1,  EmptyMassGrams = 160.078m, FullMassGrams = 259.739m, WaterTemperatureCelsius = 20.03m, AmbientTemperatureCelsius = 20.20m, AtmosphericPressureHpa = 1018.0m, RelativeHumidityPercent = 68.0m },
            new() { RunNumber = 2,  EmptyMassGrams = 160.079m, FullMassGrams = 259.738m, WaterTemperatureCelsius = 19.97m, AmbientTemperatureCelsius = 20.20m, AtmosphericPressureHpa = 1018.1m, RelativeHumidityPercent = 68.1m },
            new() { RunNumber = 3,  EmptyMassGrams = 160.079m, FullMassGrams = 259.732m, WaterTemperatureCelsius = 19.96m, AmbientTemperatureCelsius = 20.30m, AtmosphericPressureHpa = 1018.0m, RelativeHumidityPercent = 68.0m },
            new() { RunNumber = 4,  EmptyMassGrams = 160.078m, FullMassGrams = 259.735m, WaterTemperatureCelsius = 19.98m, AmbientTemperatureCelsius = 20.20m, AtmosphericPressureHpa = 1017.9m, RelativeHumidityPercent = 67.9m },
            new() { RunNumber = 5,  EmptyMassGrams = 160.081m, FullMassGrams = 259.736m, WaterTemperatureCelsius = 20.02m, AmbientTemperatureCelsius = 20.30m, AtmosphericPressureHpa = 1018.0m, RelativeHumidityPercent = 68.0m },
            new() { RunNumber = 6,  EmptyMassGrams = 160.082m, FullMassGrams = 259.738m, WaterTemperatureCelsius = 20.02m, AmbientTemperatureCelsius = 20.30m, AtmosphericPressureHpa = 1018.2m, RelativeHumidityPercent = 68.2m },
            new() { RunNumber = 7,  EmptyMassGrams = 160.080m, FullMassGrams = 259.737m, WaterTemperatureCelsius = 20.01m, AmbientTemperatureCelsius = 20.20m, AtmosphericPressureHpa = 1018.0m, RelativeHumidityPercent = 68.0m },
            new() { RunNumber = 8,  EmptyMassGrams = 160.080m, FullMassGrams = 259.740m, WaterTemperatureCelsius = 20.04m, AmbientTemperatureCelsius = 20.30m, AtmosphericPressureHpa = 1018.1m, RelativeHumidityPercent = 68.1m },
            new() { RunNumber = 9,  EmptyMassGrams = 160.078m, FullMassGrams = 259.737m, WaterTemperatureCelsius = 20.03m, AmbientTemperatureCelsius = 20.20m, AtmosphericPressureHpa = 1018.0m, RelativeHumidityPercent = 67.9m },
            new() { RunNumber = 10, EmptyMassGrams = 160.077m, FullMassGrams = 259.742m, WaterTemperatureCelsius = 20.05m, AmbientTemperatureCelsius = 20.30m, AtmosphericPressureHpa = 1018.0m, RelativeHumidityPercent = 68.0m },
            new() { RunNumber = 11, EmptyMassGrams = 160.079m, FullMassGrams = 259.740m, WaterTemperatureCelsius = 20.03m, AmbientTemperatureCelsius = 20.20m, AtmosphericPressureHpa = 1018.1m, RelativeHumidityPercent = 68.1m }
        };
    }
}
