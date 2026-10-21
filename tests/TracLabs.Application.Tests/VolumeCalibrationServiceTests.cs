using FluentAssertions;
using TracLabs.Application.DTOs.Calibration;
using TracLabs.Domain.Enums;
using TracLabs.Infrastructure.Services;
using Xunit;

namespace TracLabs.Application.Tests;

/// <summary>
/// Testes de validação metrológica do VolumeCalibrationService contra as planilhas padrão
/// de calibração volumétrica (DOC-CGCRE-027 / ISO 4787):
/// 1. Balão Volumétrico 100 mL ('planilha volume 11-08-25.xlsx')
/// 2. Picnômetro 50 mL ('planilha volume 19-10-26_picnômetro de 50 mL.xlsx')
/// 3. Balão Volumétrico 2000 mL ('planilha volume 19-10-26_Balão de 2000 mL.xlsx')
/// 4. Pipeta 1 mL ('planilha volume 19-10-26_Pipeta de 1 mL.xlsx')
/// </summary>
public class VolumeCalibrationServiceTests
{
    private readonly VolumeCalibrationService _service = new();

    [Fact]
    public void Calculate_With100MlFlaskSpreadsheet_ShouldMatchExpectedMetrologicalOutputs()
    {
        // Arrange — Dados retirados fielmente da planilha modelo 'planilha volume 11-08-25.xlsx'
        var request = new VolumeCalibrationRequest
        {
            NominalVolumeMl = 100m,
            Material = VolumetricMaterial.SodaLimeGlass,
            ThermalExpansionCoefficient = 4.2e-5m,
            ThermalExpansionUncertainty = 1.21e-6m,

            // Condições ambientais médias
            AtmosphericPressureHpa = 1018m,
            BarometerCorrectionHpa = -1.7m,
            AmbientTemperatureCelsius = 20.2m,
            AmbientThermometerCorrectionCelsius = 0.01m,
            RelativeHumidityPercent = 68m,
            HygrometerCorrectionPercent = 0.9m,

            // Padrão de massa específica da água pura
            WaterDensityReferenceGramsPerCm3 = 0.998192m,
            WaterDensityUncertaintyGramsPerCm3 = 5.06713e-5m,
            WaterDensityCoverageFactor = 2.12631m,
            WeightsDensityGramsPerCm3 = 8.0m,

            // Balança
            BalanceResolutionGrams = 0.001m,
            BalanceCertificateUncertaintyGrams = 0.001m,
            BalanceCoverageFactor = 2.0m,
            BalanceFullCorrectionGrams = -0.004m,
            BalanceEmptyCorrectionGrams = -0.003m,

            // Termômetro da água
            WaterThermometerResolutionCelsius = 0.01m,
            WaterThermometerUncertaintyCelsius = 0.005m,
            WaterThermometerCoverageFactor = 2.0m,
            WaterThermometerCorrectionCelsius = -0.01m,

            // Série de 11 pesagens repetidas da planilha
            Measurements = new List<VolumeMeasurementInputDto>
            {
                new() { RunNumber = 1, FullMassGrams = 259.739m, EmptyMassGrams = 160.078m, WaterTemperatureCelsius = 20.03m },
                new() { RunNumber = 2, FullMassGrams = 259.738m, EmptyMassGrams = 160.079m, WaterTemperatureCelsius = 19.97m },
                new() { RunNumber = 3, FullMassGrams = 259.732m, EmptyMassGrams = 160.079m, WaterTemperatureCelsius = 19.96m },
                new() { RunNumber = 4, FullMassGrams = 259.735m, EmptyMassGrams = 160.078m, WaterTemperatureCelsius = 19.98m },
                new() { RunNumber = 5, FullMassGrams = 259.736m, EmptyMassGrams = 160.081m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 6, FullMassGrams = 259.738m, EmptyMassGrams = 160.082m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 7, FullMassGrams = 259.737m, EmptyMassGrams = 160.080m, WaterTemperatureCelsius = 20.01m },
                new() { RunNumber = 8, FullMassGrams = 259.740m, EmptyMassGrams = 160.080m, WaterTemperatureCelsius = 20.04m },
                new() { RunNumber = 9, FullMassGrams = 259.737m, EmptyMassGrams = 160.078m, WaterTemperatureCelsius = 20.03m },
                new() { RunNumber = 10, FullMassGrams = 259.742m, EmptyMassGrams = 160.077m, WaterTemperatureCelsius = 20.05m },
                new() { RunNumber = 11, FullMassGrams = 259.740m, EmptyMassGrams = 160.079m, WaterTemperatureCelsius = 20.03m }
            }
        };

        // Act
        var result = _service.Calculate(request);

        // Assert — Verificação dos valores de referência da planilha
        result.Should().NotBeNull();
        result.PointsCount.Should().Be(11);

        // Volume médio V20 na planilha: 99.9431 mL
        result.MeanVolumeV20Ml.Should().BeApproximately(99.9431m, 0.001m);

        // Erro sistemático: 99.9431 - 100 = -0.0569 mL
        result.SystematicErrorMl.Should().BeApproximately(-0.0569m, 0.001m);

        // Densidade do ar na planilha: 1.200 kg/m³ = 0.001200 g/cm³
        result.AirDensityKgPerM3.Should().BeApproximately(1.200m, 0.005m);

        // Desvio padrão amostral s(V20) na planilha: 0.00264 mL
        result.RepeatabilityStandardDeviationMl.Should().BeApproximately(0.00264m, 0.0005m);

        // Incerteza combinada uc(V) na planilha: 0.00383 mL
        result.CombinedStandardUncertaintyMl.Should().BeApproximately(0.00383m, 0.0005m);

        // Fator de abrangência k na planilha: ~2.02
        result.CoverageFactor.Should().BeApproximately(2.02m, 0.05m);

        // Incerteza expandida U na planilha: 0.0077 mL
        result.ExpandedUncertaintyMl.Should().BeApproximately(0.0077m, 0.001m);

        // Orçamento de incerteza deve conter todas as fontes rastreáveis da ISO 4787 / GUM
        result.UncertaintyBudget.Count.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void Calculate_With50MlPycnometerSpreadsheet_ShouldMatchExpectedMetrologicalOutputs()
    {
        // Arrange — Dados retirados fielmente da planilha modelo 'planilha volume 19-10-26_picnômetro de 50 mL.xlsx'
        var request = new VolumeCalibrationRequest
        {
            NominalVolumeMl = 50m,
            Material = VolumetricMaterial.SodaLimeGlass,
            ThermalExpansionCoefficient = 4.2e-5m,
            ThermalExpansionUncertainty = 1.21e-6m,

            // Condições ambientais
            AtmosphericPressureHpa = 1018m,
            BarometerCorrectionHpa = -1.7m,
            AmbientTemperatureCelsius = 20.1m,
            AmbientThermometerCorrectionCelsius = 0.01m,
            RelativeHumidityPercent = 68m,
            HygrometerCorrectionPercent = 0.9m,

            // Padrão de massa específica da água
            WaterDensityReferenceGramsPerCm3 = 0.998190m,
            WaterDensityUncertaintyGramsPerCm3 = 5.0e-5m,
            WaterDensityCoverageFactor = 2.133m,
            WeightsDensityGramsPerCm3 = 8.0m,

            // Balança analítica de 4 casas decimais
            BalanceResolutionGrams = 0.0001m,
            BalanceCertificateUncertaintyGrams = 0.0002m,
            BalanceCoverageFactor = 2.0m,
            BalanceFullCorrectionGrams = -0.0003m,
            BalanceEmptyCorrectionGrams = -0.0001m,

            // Termômetro da água
            WaterThermometerResolutionCelsius = 0.01m,
            WaterThermometerUncertaintyCelsius = 0.005m,
            WaterThermometerCoverageFactor = 2.0m,
            WaterThermometerCorrectionCelsius = -0.01m,

            // 11 pesagens do picnômetro cheio e vazio
            Measurements = new List<VolumeMeasurementInputDto>
            {
                new() { RunNumber = 1, FullMassGrams = 184.6521m, EmptyMassGrams = 134.9971m, WaterTemperatureCelsius = 19.96m },
                new() { RunNumber = 2, FullMassGrams = 184.6435m, EmptyMassGrams = 134.9969m, WaterTemperatureCelsius = 19.97m },
                new() { RunNumber = 3, FullMassGrams = 184.6341m, EmptyMassGrams = 134.9969m, WaterTemperatureCelsius = 19.97m },
                new() { RunNumber = 4, FullMassGrams = 184.6418m, EmptyMassGrams = 134.9969m, WaterTemperatureCelsius = 19.98m },
                new() { RunNumber = 5, FullMassGrams = 184.6438m, EmptyMassGrams = 134.9969m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 6, FullMassGrams = 184.6462m, EmptyMassGrams = 134.9972m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 7, FullMassGrams = 184.6392m, EmptyMassGrams = 134.9971m, WaterTemperatureCelsius = 20.05m },
                new() { RunNumber = 8, FullMassGrams = 184.6375m, EmptyMassGrams = 134.9970m, WaterTemperatureCelsius = 20.05m },
                new() { RunNumber = 9, FullMassGrams = 184.6312m, EmptyMassGrams = 134.9969m, WaterTemperatureCelsius = 20.03m },
                new() { RunNumber = 10, FullMassGrams = 184.6418m, EmptyMassGrams = 134.9971m, WaterTemperatureCelsius = 20.03m },
                new() { RunNumber = 11, FullMassGrams = 184.6395m, EmptyMassGrams = 134.9970m, WaterTemperatureCelsius = 20.03m }
            }
        };

        // Act
        var result = _service.Calculate(request);

        // Assert — Verificação dos valores da planilha
        result.Should().NotBeNull();
        result.PointsCount.Should().Be(11);

        // Volume médio V20 na planilha: 49.7862 mL
        result.MeanVolumeV20Ml.Should().BeApproximately(49.7862m, 0.001m);

        // Erro sistemático: 49.7862 - 50 = -0.2138 mL
        result.SystematicErrorMl.Should().BeApproximately(-0.2138m, 0.001m);

        // Desvio padrão amostral s(V20) na planilha: 0.00576 mL
        result.RepeatabilityStandardDeviationMl.Should().BeApproximately(0.00576m, 0.0005m);

        // Incerteza combinada uc(V) na planilha: 0.00644 mL
        result.CombinedStandardUncertaintyMl.Should().BeApproximately(0.00644m, 0.0005m);

        // Fator de abrangência k na planilha: ~2.00
        result.CoverageFactor.Should().BeApproximately(2.00m, 0.05m);

        // Incerteza expandida U na planilha: 0.0129 mL
        result.ExpandedUncertaintyMl.Should().BeApproximately(0.0129m, 0.001m);
    }

    [Fact]
    public void Calculate_With2000MlFlaskSpreadsheet_ShouldMatchExpectedMetrologicalOutputs()
    {
        // Arrange — Dados retirados fielmente da planilha modelo 'planilha volume 19-10-26_Balão de 2000 mL.xlsx'
        var request = new VolumeCalibrationRequest
        {
            NominalVolumeMl = 2000m,
            Material = VolumetricMaterial.BorosilicateGlass33,
            ThermalExpansionCoefficient = 1.8e-5m,
            ThermalExpansionUncertainty = 8.0e-6m,

            // Condições ambientais médias
            AtmosphericPressureHpa = 1014m,
            BarometerCorrectionHpa = -1.7m,
            AmbientTemperatureCelsius = 20.3m,
            AmbientThermometerCorrectionCelsius = 0.01m,
            RelativeHumidityPercent = 58.45m,
            HygrometerCorrectionPercent = 0.9m,

            // Padrão de massa específica da água pura
            WaterDensityReferenceGramsPerCm3 = 0.998302m,
            WaterDensityUncertaintyGramsPerCm3 = 7.0e-5m,
            WaterDensityCoverageFactor = 2.12631m,
            WeightsDensityGramsPerCm3 = 8.0m,

            // Balança (método com tara zerada diretamente na balança)
            BalanceResolutionGrams = 0m,
            BalanceCertificateUncertaintyGrams = 0.0001m,
            BalanceCoverageFactor = 2.0m,
            BalanceFullCorrectionGrams = -0.0001m,
            BalanceEmptyCorrectionGrams = 0m,

            // Termômetro da água
            WaterThermometerResolutionCelsius = 0.02m,
            WaterThermometerUncertaintyCelsius = 0.009m,
            WaterThermometerCoverageFactor = 2.0m,
            WaterThermometerCorrectionCelsius = -0.021m,

            // Série de 11 pesagens com recipiente tarado (EmptyMassGrams = 0)
            Measurements = new List<VolumeMeasurementInputDto>
            {
                new() { RunNumber = 1, FullMassGrams = 1994.437m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 2, FullMassGrams = 1994.469m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 3, FullMassGrams = 1994.493m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 4, FullMassGrams = 1994.560m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 5, FullMassGrams = 1994.478m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 6, FullMassGrams = 1994.471m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 7, FullMassGrams = 1994.467m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 8, FullMassGrams = 1994.459m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.01m },
                new() { RunNumber = 9, FullMassGrams = 1994.435m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.01m },
                new() { RunNumber = 10, FullMassGrams = 1994.421m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 11, FullMassGrams = 1994.401m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 19.98m }
            }
        };

        // Act
        var result = _service.Calculate(request);

        // Assert — Verificação dos valores de referência da planilha
        result.Should().NotBeNull();
        result.PointsCount.Should().Be(11);

        // Volume médio V20 na planilha: 1999.9522 mL
        result.MeanVolumeV20Ml.Should().BeApproximately(1999.9522m, 0.01m);

        // Erro sistemático: 1999.9522 - 2000 = -0.0478 mL
        result.SystematicErrorMl.Should().BeApproximately(-0.0478m, 0.01m);

        // Desvio padrão amostral s(V20) na planilha: 0.04199 mL
        result.RepeatabilityStandardDeviationMl.Should().BeApproximately(0.04199m, 0.001m);

        // Incerteza combinada uc(V) na planilha: 0.08129 mL
        result.CombinedStandardUncertaintyMl.Should().BeApproximately(0.08129m, 0.001m);

        // Fator de abrangência k na planilha: ~2.05
        result.CoverageFactor.Should().BeApproximately(2.05m, 0.05m);

        // Incerteza expandida U na planilha: 0.1669 mL
        result.ExpandedUncertaintyMl.Should().BeApproximately(0.1669m, 0.005m);
    }

    [Fact]
    public void Calculate_With1MlPipetteSpreadsheet_ShouldMatchExpectedMetrologicalOutputs()
    {
        // Arrange — Dados retirados fielmente da planilha modelo 'planilha volume 19-10-26_Pipeta de 1 mL.xlsx'
        var request = new VolumeCalibrationRequest
        {
            NominalVolumeMl = 1m,
            Material = VolumetricMaterial.BorosilicateGlass50,
            ThermalExpansionCoefficient = 1.5e-5m,
            ThermalExpansionUncertainty = 8.66e-6m,

            // Condições ambientais médias
            AtmosphericPressureHpa = 1014m,
            BarometerCorrectionHpa = -1.6m,
            AmbientTemperatureCelsius = 20.3m,
            AmbientThermometerCorrectionCelsius = 0.01m,
            RelativeHumidityPercent = 58.45m,
            HygrometerCorrectionPercent = -0.1m,

            // Padrão de massa específica da água pura
            WaterDensityReferenceGramsPerCm3 = 0.998197m,
            WaterDensityUncertaintyGramsPerCm3 = 6.0e-5m,
            WaterDensityCoverageFactor = 2.12631m,
            WeightsDensityGramsPerCm3 = 8.0m,

            // Balança (método por escoamento com recipiente receptor tarado)
            BalanceResolutionGrams = 0m,
            BalanceCertificateUncertaintyGrams = 0.0001m,
            BalanceCoverageFactor = 2.0m,
            BalanceFullCorrectionGrams = -0.0001m,
            BalanceEmptyCorrectionGrams = 0m,

            // Termômetro da água
            WaterThermometerResolutionCelsius = 0.02m,
            WaterThermometerUncertaintyCelsius = 0.009m,
            WaterThermometerCoverageFactor = 2.0m,
            WaterThermometerCorrectionCelsius = -0.021m,

            // Série de 11 pesagens do volume escoado (EmptyMassGrams = 0)
            Measurements = new List<VolumeMeasurementInputDto>
            {
                new() { RunNumber = 1, FullMassGrams = 0.9950m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 2, FullMassGrams = 0.9933m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 3, FullMassGrams = 0.9920m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 4, FullMassGrams = 0.9938m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 5, FullMassGrams = 0.9934m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 6, FullMassGrams = 0.9958m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 7, FullMassGrams = 0.9973m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 8, FullMassGrams = 0.9964m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.01m },
                new() { RunNumber = 9, FullMassGrams = 0.9961m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.01m },
                new() { RunNumber = 10, FullMassGrams = 0.9977m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 20.02m },
                new() { RunNumber = 11, FullMassGrams = 0.9960m, EmptyMassGrams = 0m, WaterTemperatureCelsius = 19.98m }
            }
        };

        // Act
        var result = _service.Calculate(request);

        // Assert — Verificação dos valores de referência da planilha
        result.Should().NotBeNull();
        result.PointsCount.Should().Be(11);

        // Volume médio V20 na planilha: 0.9979 mL
        result.MeanVolumeV20Ml.Should().BeApproximately(0.9979m, 0.0005m);

        // Erro sistemático: 0.9979 - 1 = -0.0021 mL
        result.SystematicErrorMl.Should().BeApproximately(-0.0021m, 0.0005m);

        // Desvio padrão amostral s(V20) na planilha: 0.00182 mL
        result.RepeatabilityStandardDeviationMl.Should().BeApproximately(0.00182m, 0.0005m);

        // Incerteza combinada uc(V) na planilha: 0.00174 mL
        result.CombinedStandardUncertaintyMl.Should().BeApproximately(0.00174m, 0.0005m);

        // Fator de abrangência k na planilha: ~2.00
        result.CoverageFactor.Should().BeApproximately(2.00m, 0.05m);

        // Incerteza expandida U na planilha: 0.00349 mL
        result.ExpandedUncertaintyMl.Should().BeApproximately(0.00349m, 0.0005m);
    }
}
