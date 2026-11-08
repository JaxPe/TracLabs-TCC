using FluentAssertions;
using TracLabs.Frontend.Models;
using TracLabs.Frontend.Models.Enums;
using TracLabs.Frontend.Services;
using Xunit;

namespace TracLabs.Frontend.Tests;

public class MetrologyCalculationsM4Tests
{
    [Fact]
    public void ManualWaterDensity_WhenProvided_OverridesTanakaCalculation()
    {
        var record = new CalibrationRecordDto
        {
            NominalCapacity = 100.0m,
            MaxPermissibleError = 0.100m,
            AccuracyClass = "Classe A",
            Material = VolumetricMaterial.SodaLimeGlass,
            WaterTemperatureCelsius = 20.0m,
            WaterDensityGcm3 = 0.998205m, // Experimental density from pycnometer
            PycnometerStandardId = "pad-pic-01",
            BalanceStandardId = "pad-bal-01",
            LiquidThermometerStandardId = "pad-term-01",
            BarometerStandardId = "pad-bar-01",
            ThermoHygrometerStandardId = "pad-hig-01",
            Measurements = new List<VolumeMeasurementPointDto>
            {
                new() { RunNumber = 1, EmptyMassGrams = 100.000m, FullMassGrams = 199.820m, WaterTemperatureCelsius = 20.0m }
            }
        };

        MetrologyCalculations.ComputeCalibrationResults(record, 100.0m, 0.100m);

        // Verification: record water density must equal manual value exactly
        record.CalculatedWaterDensity.Should().Be(0.998205m);
        record.IsManualWaterDensity.Should().BeTrue();
        record.RequiresPycnometer.Should().BeTrue();
        record.HasValidPycnometer.Should().BeTrue();
        record.HasValidStandards().Should().BeTrue();
    }

    [Fact]
    public void PerPointAmbientConditions_ComputesIndividualAirDensityAndZFactor()
    {
        var record = new CalibrationRecordDto
        {
            NominalCapacity = 100.0m,
            MaxPermissibleError = 0.100m,
            Material = VolumetricMaterial.BorosilicateGlass33,
            WaterTemperatureCelsius = 20.0m,
            AmbientTemperatureCelsius = 20.0m,
            AtmosphericPressureHpa = 1013.25m,
            RelativeHumidityPercent = 50.0m,
            Measurements = new List<VolumeMeasurementPointDto>
            {
                new()
                {
                    RunNumber = 1,
                    EmptyMassGrams = 50.0m,
                    FullMassGrams = 149.80m,
                    WaterTemperatureCelsius = 19.5m,
                    AmbientTemperatureCelsius = 20.0m,
                    AtmosphericPressureHpa = 1013.0m,
                    RelativeHumidityPercent = 50.0m
                },
                new()
                {
                    RunNumber = 2,
                    EmptyMassGrams = 50.0m,
                    FullMassGrams = 149.81m,
                    WaterTemperatureCelsius = 20.5m,
                    AmbientTemperatureCelsius = 22.0m,
                    AtmosphericPressureHpa = 1020.0m,
                    RelativeHumidityPercent = 65.0m
                }
            }
        };

        MetrologyCalculations.ComputeCalibrationResults(record, 100.0m, 0.100m);

        var pt1 = record.Measurements[0];
        var pt2 = record.Measurements[1];

        pt1.CalculatedAirDensity.Should().BeGreaterThan(0m);
        pt2.CalculatedAirDensity.Should().BeGreaterThan(0m);
        pt1.CalculatedZFactor.Should().BeGreaterThan(1.00m);
        pt2.CalculatedZFactor.Should().BeGreaterThan(1.00m);

        // Point 2 with higher pressure and higher temperature has distinct air density
        pt1.CalculatedAirDensity.Should().NotBe(pt2.CalculatedAirDensity);
        pt1.CalculatedZFactor.Should().NotBe(pt2.CalculatedZFactor);
    }

    [Fact]
    public void PerPointAmbientConditions_WhenPointConditionsZero_FallsBackToRecordConditions()
    {
        var record = new CalibrationRecordDto
        {
            NominalCapacity = 100.0m,
            MaxPermissibleError = 0.100m,
            AmbientTemperatureCelsius = 21.5m,
            AtmosphericPressureHpa = 1015.0m,
            RelativeHumidityPercent = 55.0m,
            Measurements = new List<VolumeMeasurementPointDto>
            {
                new()
                {
                    RunNumber = 1,
                    EmptyMassGrams = 50.0m,
                    FullMassGrams = 150.0m,
                    AmbientTemperatureCelsius = 0m,
                    AtmosphericPressureHpa = 0m,
                    RelativeHumidityPercent = 0m
                }
            }
        };

        MetrologyCalculations.ComputeCalibrationResults(record, 100.0m, 0.100m);

        var pt = record.Measurements[0];
        var expectedAirDensity = Math.Round(MetrologyCalculations.CalculateAirDensity(1015.0m, 21.5m, 55.0m), 4);
        pt.CalculatedAirDensity.Should().Be(expectedAirDensity);
    }

    [Theory]
    [InlineData(true, true, true, true, false, null, true)]       // Combined thermo-hygro
    [InlineData(true, true, true, false, true, null, true)]       // Separate ambient + hygro
    [InlineData(false, true, true, true, false, null, false)]     // Missing balance
    [InlineData(true, false, true, true, false, null, false)]     // Missing thermometer
    [InlineData(true, true, false, true, false, null, false)]     // Missing barometer
    [InlineData(true, true, true, false, false, null, false)]     // Neither thermo-hygro nor separate
    [InlineData(true, true, true, true, false, 0.9982, false)]    // Manual density without pycnometer
    [InlineData(true, true, true, true, false, 0.9982, true, "pad-pic-01")] // Manual density with pycnometer
    public void HasValidStandards_EnforcesIsoStandardsRules(
        bool hasBalance,
        bool hasThermometer,
        bool hasBarometer,
        bool hasThermoHygro,
        bool hasSeparateAmbient,
        double? manualDensity,
        bool expectedValid,
        string? pycnometerId = null)
    {
        var record = new CalibrationRecordDto
        {
            BalanceStandardId = hasBalance ? "pad-bal-01" : "",
            LiquidThermometerStandardId = hasThermometer ? "pad-term-01" : "",
            BarometerStandardId = hasBarometer ? "pad-bar-01" : "",
            ThermoHygrometerStandardId = hasThermoHygro ? "pad-hig-01" : "",
            AmbientThermometerStandardId = hasSeparateAmbient ? "pad-teramb-01" : "",
            HygrometerStandardId = hasSeparateAmbient ? "pad-hig-02" : "",
            WaterDensityGcm3 = manualDensity.HasValue ? (decimal)manualDensity.Value : null,
            PycnometerStandardId = pycnometerId
        };

        record.HasValidStandards().Should().Be(expectedValid);
    }

    [Fact]
    public void ThermometerStandardId_PropertyAlias_MaintainsFullSynchronization()
    {
        var record = new CalibrationRecordDto();

        record.ThermometerStandardId = "pad-term-legacy";
        record.LiquidThermometerStandardId.Should().Be("pad-term-legacy");

        record.LiquidThermometerStandardId = "pad-term-new";
        record.ThermometerStandardId.Should().Be("pad-term-new");
    }

    [Fact]
    public void Benchmark100MlPoints_AllPointsWithinToleranceAndZeroCapacityFallbackWorks()
    {
        var record = new CalibrationRecordDto
        {
            NominalCapacity = 0m,
            MaxPermissibleError = 0m,
            AccuracyClass = "Classe A",
            Measurements = MetrologyCalculations.GetBenchmark100MlPoints()
        };

        // When passing 0m, ComputeCalibrationResults should fallback to 100mL and ISO 1042 EMA 0.100mL
        MetrologyCalculations.ComputeCalibrationResults(record, 0m, 0m);

        record.NominalCapacity.Should().Be(100.0m);
        record.MaxPermissibleError.Should().Be(0.100m);
        record.Result.Should().Be(CalibrationResult.Conforming);

        // Every single point in the verified real dataset must have |DeviationMl| <= EMA (0.100 mL)
        foreach (var pt in record.Measurements)
        {
            Math.Abs(pt.DeviationMl).Should().BeLessThanOrEqualTo(record.MaxPermissibleError,
                $"point run #{pt.RunNumber} must not be flagged as error in benchmark measurement");
        }
    }
}