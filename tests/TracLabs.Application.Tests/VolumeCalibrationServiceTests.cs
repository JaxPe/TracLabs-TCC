using FluentAssertions;
using TracLabs.Application.DTOs.Calibration;
using TracLabs.Domain.Enums;
using TracLabs.Infrastructure.Services;
using Xunit;

namespace TracLabs.Application.Tests;

/// <summary>
/// Testes de validação metrológica do VolumeCalibrationService contra a planilha padrão
/// de calibração volumétrica (DOC-CGCRE-027 / ISO 4787).
/// </summary>
public class VolumeCalibrationServiceTests
{
    private readonly VolumeCalibrationService _service = new();

    [Fact]
    public void Calculate_WithSpreadsheetData_ShouldMatchExpectedMetrologicalOutputs()
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
}
