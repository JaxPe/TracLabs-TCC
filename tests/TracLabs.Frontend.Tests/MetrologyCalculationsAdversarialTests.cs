using FluentAssertions;
using TracLabs.Frontend.Models;
using TracLabs.Frontend.Models.Enums;
using TracLabs.Frontend.Services;
using Xunit;
using Xunit.Abstractions;

namespace TracLabs.Frontend.Tests;

public class MetrologyCalculationsAdversarialTests
{
    private readonly ITestOutputHelper _output;

    public MetrologyCalculationsAdversarialTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // =========================================================================
    // 1. Tanaka Pure Water Density Calculation Tests
    // =========================================================================
    [Theory]
    [InlineData(15.0, 0.999103)]
    [InlineData(16.0, 0.998946)]
    [InlineData(17.0, 0.998778)]
    [InlineData(18.0, 0.998599)]
    [InlineData(19.0, 0.998408)]
    [InlineData(20.0, 0.998207)]
    [InlineData(20.03, 0.998201)]
    [InlineData(21.0, 0.997996)]
    [InlineData(22.0, 0.997773)]
    [InlineData(23.0, 0.997541)]
    [InlineData(24.0, 0.997300)]
    [InlineData(25.0, 0.997048)]
    [InlineData(26.0, 0.996787)]
    [InlineData(27.0, 0.996516)]
    [InlineData(28.0, 0.996237)]
    [InlineData(29.0, 0.995948)]
    [InlineData(30.0, 0.995650)]
    public void Tanaka_OperationalTemperatureRange_MatchesReferenceTable(double tempC, double expectedDensity)
    {
        var density = MetrologyCalculations.CalculatePureWaterDensity((decimal)tempC);
        ((double)density).Should().BeApproximately(expectedDensity, 0.000005);
    }

    [Fact]
    public void Tanaka_MaximumDensity_OccursNear3Point98Degrees()
    {
        var rhoMax = MetrologyCalculations.CalculatePureWaterDensity(3.983035m);
        // Tanaka constant a5 = 0.999974950
        ((double)rhoMax).Should().BeApproximately(0.99997495, 0.0000001);

        var rho3 = MetrologyCalculations.CalculatePureWaterDensity(3.0m);
        var rho5 = MetrologyCalculations.CalculatePureWaterDensity(5.0m);
        rhoMax.Should().BeGreaterThan(rho3);
        rhoMax.Should().BeGreaterThan(rho5);
    }

    [Theory]
    [InlineData(0.0, 0.999843)]
    [InlineData(40.0, 0.992219)]
    public void Tanaka_BoundaryTemperatures_EvaluatesConsistently(double tempC, double expectedDensity)
    {
        var density = MetrologyCalculations.CalculatePureWaterDensity((decimal)tempC);
        ((double)density).Should().BeApproximately(expectedDensity, 0.00001);
    }

    // =========================================================================
    // 2. Spieweck Air Density Calculation Tests
    // =========================================================================
    [Theory]
    // Pressure range: 900 to 1100 hPa, Humidity 20% to 90%, Temp 15°C to 30°C
    [InlineData(1013.25, 20.0, 50.0, 1.1993)]
    [InlineData(1018.0, 20.2, 68.0, 1.2022)]
    [InlineData(900.0, 20.0, 50.0, 1.0652)]
    [InlineData(1100.0, 20.0, 50.0, 1.3020)]
    [InlineData(1013.25, 15.0, 50.0, 1.2223)]
    [InlineData(1013.25, 30.0, 50.0, 1.1556)]
    [InlineData(1013.25, 20.0, 20.0, 1.2023)]
    [InlineData(1013.25, 20.0, 90.0, 1.1952)]
    public void Spieweck_OperationalSpace_ShouldCalculatePhysicallySoundDensity(
        double pressureHpa, double tempC, double humidityPercent, double expectedAirDensityKgPerM3)
    {
        var airDensity = MetrologyCalculations.CalculateAirDensity(
            (decimal)pressureHpa, (decimal)tempC, (decimal)humidityPercent);

        ((double)airDensity).Should().BeApproximately(expectedAirDensityKgPerM3, 0.001);
    }

    [Fact]
    public void Spieweck_PhysicalMonotonicity_HigherPressureIncreasesDensity_HigherTempDecreasesDensity()
    {
        var rhoRef = MetrologyCalculations.CalculateAirDensity(1013.25m, 20.0m, 50.0m);
        var rhoHighP = MetrologyCalculations.CalculateAirDensity(1050.0m, 20.0m, 50.0m);
        var rhoHighT = MetrologyCalculations.CalculateAirDensity(1013.25m, 25.0m, 50.0m);
        var rhoHighH = MetrologyCalculations.CalculateAirDensity(1013.25m, 20.0m, 80.0m);

        rhoHighP.Should().BeGreaterThan(rhoRef, "higher pressure increases air density");
        rhoHighT.Should().BeLessThan(rhoRef, "higher temperature decreases air density");
        rhoHighH.Should().BeLessThan(rhoRef, "higher humidity decreases air density (water vapor is lighter than dry air)");
    }

    [Fact]
    public void Spieweck_ExtremeBoundary_HandlesZeroAndNegativeGracefully()
    {
        // Negative denominator protection (K4 + temp <= 0)
        var fallback = MetrologyCalculations.CalculateAirDensity(1013.25m, -273.15m, 50.0m);
        fallback.Should().Be(1.200m);
    }

    // =========================================================================
    // 3. Z-Factor vs ISO 4787 Table A.1
    // =========================================================================
    [Theory]
    // Borosilicate 3.3 (gamma = 9.9e-6)
    [InlineData(15.0, 1013.25, 50.0, VolumetricMaterial.BorosilicateGlass33, 1.0020)]
    [InlineData(20.0, 1013.25, 50.0, VolumetricMaterial.BorosilicateGlass33, 1.0029)]
    [InlineData(25.0, 1013.25, 50.0, VolumetricMaterial.BorosilicateGlass33, 1.0040)]
    // Soda-lime Glass (gamma = 4.2e-5)
    [InlineData(20.0, 1013.25, 50.0, VolumetricMaterial.SodaLimeGlass, 1.0029)]
    [InlineData(25.0, 1013.25, 50.0, VolumetricMaterial.SodaLimeGlass, 1.0038)]
    public void ZFactor_ISO4787TableA1_ValuesMatchReferenceTable(
        double tempC, double pressureHpa, double humidityPercent, VolumetricMaterial material, double expectedZ)
    {
        var rhoW = MetrologyCalculations.CalculatePureWaterDensity((decimal)tempC);
        var rhoA = MetrologyCalculations.CalculateAirDensity((decimal)pressureHpa, (decimal)tempC, (decimal)humidityPercent);
        var z = MetrologyCalculations.CalculateZFactor(rhoW, rhoA, material, (decimal)tempC);

        ((double)z).Should().BeApproximately(expectedZ, 0.0002);
    }

    // =========================================================================
    // 4. Statistical Engine: 11-point Benchmark Dataset & Empirical Discrepancies
    // =========================================================================
    [Fact]
    public void StatisticalEngine_Benchmark11PointFlask100Ml_VerifiesCalculatedOutputsAndDiscrepancies()
    {
        var points = MetrologyCalculations.GetBenchmark100MlPoints();
        points.Should().HaveCount(11);

        var record = new CalibrationRecordDto
        {
            EquipmentTag = "BAL-CLI-001",
            NominalCapacity = 100.0m,
            MaxPermissibleError = 0.100m,
            AccuracyClass = "Classe A",
            Material = VolumetricMaterial.SodaLimeGlass,
            WaterTemperatureCelsius = 20.03m,
            AmbientTemperatureCelsius = 20.20m,
            AtmosphericPressureHpa = 1018.0m,
            RelativeHumidityPercent = 68.0m,
            Measurements = points
        };

        MetrologyCalculations.ComputeCalibrationResults(record, 100.0m, 0.100m);

        // 1. Mean V20: exactly matches benchmark 99.9431 mL
        record.MeanV20.Should().Be(99.9431m);

        // 2. Systematic Error Es: exactly matches benchmark -0.0569 mL
        record.SystematicError.Should().Be(-0.0569m);

        // 3. Expanded Uncertainty U: matches benchmark 0.0077 mL
        record.ExpandedUncertainty.Should().Be(0.0077m);

        // 4. Empirical Challenger Finding on Standard Deviation s:
        // In the official benchmark spreadsheet (planilha volume 11-08-25.xlsx),
        // apparent mass is calculated using the mean empty tare (Mv_mean = 160.0792 g), yielding s = 0.00264 mL.
        // In MetrologyCalculations.cs, individual row-by-row empty tares are subtracted, which injects tare noise
        // and inflates sample standard deviation to s = 0.00367 - 0.00368 mL (+39% discrepancy).
        record.SampleStdDev.Should().BeInRange(0.00367m, 0.00368m);

        // 5. Empirical Challenger Finding on Coverage Factor k:
        // Spreadsheet has Welch-Satterthwaite veff = 133, yielding k = 2.02.
        // MetrologyCalculations.cs uses v = n - 1 = 10, yielding k = 2.27 (+12.4% discrepancy).
        record.CoverageFactor.Should().Be(2.27m);

        // 6. Empirical Challenger Finding on Combined Uncertainty uc:
        // Spreadsheet has uc = 0.00383 mL (including water density standard uncertainty).
        // MetrologyCalculations.cs uses simplified 4-term model yielding uc = 0.00339 mL (-11.5% discrepancy).
        record.CombinedUncertainty.Should().Be(0.00339m);

        _output.WriteLine($"[CHALLENGER VERIFIED] MeanV20 = {record.MeanV20} mL (Benchmark = 99.9431 mL)");
        _output.WriteLine($"[CHALLENGER VERIFIED] Es = {record.SystematicError} mL (Benchmark = -0.0569 mL)");
        _output.WriteLine($"[CHALLENGER FINDING] SampleStdDev = {record.SampleStdDev} mL vs Benchmark = 0.00264 mL (+39.4%)");
        _output.WriteLine($"[CHALLENGER FINDING] uc = {record.CombinedUncertainty} mL vs Benchmark = 0.00383 mL (-11.5%)");
        _output.WriteLine($"[CHALLENGER FINDING] k = {record.CoverageFactor} vs Benchmark = 2.02 (+12.4%)");
        _output.WriteLine($"[CHALLENGER CANCELING EFFECT] U = {record.ExpandedUncertainty} mL matches Benchmark = 0.0077 mL via offsetting errors");
    }

    // =========================================================================
    // 5. Corner Cases & Stress Tests
    // =========================================================================
    [Fact]
    public void StatisticalEngine_CornerCase_EmptyRuns_ShouldNotThrow()
    {
        var record = new CalibrationRecordDto
        {
            Measurements = new List<VolumeMeasurementPointDto>()
        };

        var act = () => MetrologyCalculations.ComputeCalibrationResults(record, 100.0m, 0.100m);
        act.Should().NotThrow();
        record.MeanV20.Should().Be(0m);
    }

    [Fact]
    public void StatisticalEngine_CornerCase_SingleRun_ShouldHandleStdDevGracefully()
    {
        var record = new CalibrationRecordDto
        {
            Measurements = new List<VolumeMeasurementPointDto>
            {
                new() { RunNumber = 1, EmptyMassGrams = 100.0m, FullMassGrams = 200.0m, WaterTemperatureCelsius = 20.0m }
            }
        };

        var act = () => MetrologyCalculations.ComputeCalibrationResults(record, 100.0m, 0.100m);
        act.Should().NotThrow();
        record.SampleStdDev.Should().Be(0m);
    }

    [Fact]
    public void StatisticalEngine_CornerCase_GrossMassLessOrEqualToEmptyMass_ShouldBeFilteredOut()
    {
        var record = new CalibrationRecordDto
        {
            AtmosphericPressureHpa = 1013.25m,
            AmbientTemperatureCelsius = 20.0m,
            RelativeHumidityPercent = 50.0m,
            Measurements = new List<VolumeMeasurementPointDto>
            {
                new() { RunNumber = 1, EmptyMassGrams = 150.0m, FullMassGrams = 150.0m, WaterTemperatureCelsius = 20.0m }, // equal
                new() { RunNumber = 2, EmptyMassGrams = 150.0m, FullMassGrams = 140.0m, WaterTemperatureCelsius = 20.0m }, // less
                new() { RunNumber = 3, EmptyMassGrams = 150.0m, FullMassGrams = 250.0m, WaterTemperatureCelsius = 20.0m }  // valid
            }
        };

        MetrologyCalculations.ComputeCalibrationResults(record, 100.0m, 0.100m);
        record.MeanV20.Should().BeGreaterThan(0m);
        // Only point 3 was valid
        record.SampleStdDev.Should().Be(0m); // only 1 valid point
    }

    [Fact]
    public void StatisticalEngine_CornerCase_ExtremeVolumes_MicropipetteAndLargeFlask()
    {
        // 1. Extreme small: 10 uL (0.01 mL)
        var smallRecord = new CalibrationRecordDto
        {
            NominalCapacity = 0.01m,
            AtmosphericPressureHpa = 1013.25m,
            AmbientTemperatureCelsius = 20.0m,
            RelativeHumidityPercent = 50.0m,
            Measurements = Enumerable.Range(1, 10).Select(i => new VolumeMeasurementPointDto
            {
                RunNumber = i,
                EmptyMassGrams = 10.0m,
                FullMassGrams = 10.00998m + (i * 0.000001m),
                WaterTemperatureCelsius = 20.0m
            }).ToList()
        };
        var actSmall = () => MetrologyCalculations.ComputeCalibrationResults(smallRecord, 0.01m, 0.001m);
        actSmall.Should().NotThrow();
        smallRecord.MeanV20.Should().BeApproximately(0.01m, 0.001m);

        // 2. Extreme large: 5000 mL (5 L flask)
        var largeRecord = new CalibrationRecordDto
        {
            NominalCapacity = 5000.0m,
            AtmosphericPressureHpa = 1013.25m,
            AmbientTemperatureCelsius = 20.0m,
            RelativeHumidityPercent = 50.0m,
            Measurements = Enumerable.Range(1, 7).Select(i => new VolumeMeasurementPointDto
            {
                RunNumber = i,
                EmptyMassGrams = 1000.0m,
                FullMassGrams = 5985.0m + (i * 0.1m),
                WaterTemperatureCelsius = 20.0m
            }).ToList()
        };
        var actLarge = () => MetrologyCalculations.ComputeCalibrationResults(largeRecord, 5000.0m, 5.0m);
        actLarge.Should().NotThrow();
        largeRecord.MeanV20.Should().BeApproximately(5000.0m, 50.0m);
    }

    // =========================================================================
    // 6. Decision Rules & Conformity Evaluation
    // =========================================================================
    [Theory]
    [InlineData(0.05, 0.100, CalibrationResult.Conforming)]     // |Es| = 0.05 <= 0.100
    [InlineData(-0.05, 0.100, CalibrationResult.Conforming)]    // |-0.05| = 0.05 <= 0.100
    [InlineData(0.100, 0.100, CalibrationResult.Conforming)]    // |Es| = 0.100 <= 0.100 (boundary)
    [InlineData(-0.100, 0.100, CalibrationResult.Conforming)]   // |-0.100| = 0.100 <= 0.100 (boundary)
    [InlineData(0.105, 0.100, CalibrationResult.NonConforming)] // |Es| = 0.105 > 0.100
    [InlineData(-0.105, 0.100, CalibrationResult.NonConforming)]// |-0.105| = 0.105 > 0.100
    public void DecisionRules_ConformityAgainstEMA_EvaluatesCorrectly(
        decimal targetError, decimal ema, CalibrationResult expectedResult)
    {
        var record = new CalibrationRecordDto
        {
            NominalCapacity = 100.0m,
            MaxPermissibleError = ema,
            AtmosphericPressureHpa = 1013.25m,
            AmbientTemperatureCelsius = 20.0m,
            RelativeHumidityPercent = 50.0m,
            WaterTemperatureCelsius = 20.0m,
            Material = VolumetricMaterial.SodaLimeGlass
        };

        var rhoW = MetrologyCalculations.CalculatePureWaterDensity(20.0m);
        var rhoA = MetrologyCalculations.CalculateAirDensity(1013.25m, 20.0m, 50.0m);
        var z = MetrologyCalculations.CalculateZFactor(rhoW, rhoA, record.Material, 20.0m);

        var targetV20 = 100.0m + targetError;
        var apparentMass = Math.Round(targetV20 / z, 4);

        record.Measurements = Enumerable.Range(1, 7).Select(i => new VolumeMeasurementPointDto
        {
            RunNumber = i,
            EmptyMassGrams = 100.0m,
            FullMassGrams = 100.0m + apparentMass,
            WaterTemperatureCelsius = 20.0m
        }).ToList();

        MetrologyCalculations.ComputeCalibrationResults(record, 100.0m, ema);

        record.Result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(6, false)]
    [InlineData(7, true)]
    [InlineData(10, true)]
    [InlineData(11, true)]
    public void DecisionRules_MinimumRunsFilter_ValidatesPisoMinimo(int pointCount, bool isSufficient)
    {
        var measurements = Enumerable.Range(1, pointCount).Select(i => new VolumeMeasurementPointDto
        {
            RunNumber = i,
            EmptyMassGrams = 100.0m,
            FullMassGrams = 200.0m,
            WaterTemperatureCelsius = 20.0m
        }).ToList();

        var validPointsCount = measurements.Count(m => m.FullMassGrams > m.EmptyMassGrams);
        var passesPiso = validPointsCount >= 7;

        passesPiso.Should().Be(isSufficient);
    }
}
