using FluentAssertions;
using TracLabs.Frontend.Models;
using TracLabs.Frontend.Models.Enums;
using TracLabs.Frontend.Services;
using Xunit;

namespace TracLabs.Frontend.Tests;

/// <summary>
/// Empirical Challenger Test Suite for Milestone M4 Metrology Engine.
/// Stress tests per-point ambient drift, boundary fallbacks, Tanaka vs pycnometer density,
/// ISO standard selection validation rules, and mathematical stability under extreme edge conditions.
/// </summary>
public class MetrologyCalculationsEmpiricalChallengeTests
{
    #region 1. Per-Point Ambient Variations & Drift

    [Fact]
    public void PerPointAmbientVariation_TemperatureDrift_MonotonicallyDecreasesAirDensity()
    {
        // Spieweck formula: as ambient temperature rises, air density decreases
        var basePressure = 1013.25m;
        var baseHumidity = 50.0m;

        var rho19 = MetrologyCalculations.CalculateAirDensity(basePressure, 19.0m, baseHumidity);
        var rho20 = MetrologyCalculations.CalculateAirDensity(basePressure, 20.0m, baseHumidity);
        var rho21 = MetrologyCalculations.CalculateAirDensity(basePressure, 21.0m, baseHumidity);
        var rho25 = MetrologyCalculations.CalculateAirDensity(basePressure, 25.0m, baseHumidity);

        rho19.Should().BeGreaterThan(rho20);
        rho20.Should().BeGreaterThan(rho21);
        rho21.Should().BeGreaterThan(rho25);
    }

    [Fact]
    public void PerPointAmbientVariation_PressureDrift_MonotonicallyIncreasesAirDensity()
    {
        // Spieweck formula: as barometric pressure rises, air density increases
        var baseTemp = 20.0m;
        var baseHumidity = 50.0m;

        var rho980 = MetrologyCalculations.CalculateAirDensity(980.0m, baseTemp, baseHumidity);
        var rho1000 = MetrologyCalculations.CalculateAirDensity(1000.0m, baseTemp, baseHumidity);
        var rho1013 = MetrologyCalculations.CalculateAirDensity(1013.25m, baseTemp, baseHumidity);
        var rho1030 = MetrologyCalculations.CalculateAirDensity(1030.0m, baseTemp, baseHumidity);

        rho980.Should().BeLessThan(rho1000);
        rho1000.Should().BeLessThan(rho1013);
        rho1013.Should().BeLessThan(rho1030);
    }

    [Fact]
    public void PerPointAmbientVariation_HumidityDrift_MonotonicallyDecreasesAirDensity()
    {
        // Spieweck formula: humid air is less dense than dry air
        var basePressure = 1013.25m;
        var baseTemp = 20.0m;

        var rho30 = MetrologyCalculations.CalculateAirDensity(basePressure, baseTemp, 30.0m);
        var rho50 = MetrologyCalculations.CalculateAirDensity(basePressure, baseTemp, 50.0m);
        var rho70 = MetrologyCalculations.CalculateAirDensity(basePressure, baseTemp, 70.0m);
        var rho90 = MetrologyCalculations.CalculateAirDensity(basePressure, baseTemp, 90.0m);

        rho30.Should().BeGreaterThan(rho50);
        rho50.Should().BeGreaterThan(rho70);
        rho70.Should().BeGreaterThan(rho90);
    }

    [Fact]
    public void PerPointAmbientVariation_DifferentPoints_ProduceDistinctCalculatedValues()
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
                new() { RunNumber = 1, EmptyMassGrams = 100m, FullMassGrams = 199.8m, WaterTemperatureCelsius = 19.8m, AmbientTemperatureCelsius = 19.5m, AtmosphericPressureHpa = 1010.0m, RelativeHumidityPercent = 45m },
                new() { RunNumber = 2, EmptyMassGrams = 100m, FullMassGrams = 199.8m, WaterTemperatureCelsius = 20.0m, AmbientTemperatureCelsius = 20.2m, AtmosphericPressureHpa = 1014.0m, RelativeHumidityPercent = 55m },
                new() { RunNumber = 3, EmptyMassGrams = 100m, FullMassGrams = 199.8m, WaterTemperatureCelsius = 20.3m, AmbientTemperatureCelsius = 21.0m, AtmosphericPressureHpa = 1020.0m, RelativeHumidityPercent = 65m },
            }
        };

        MetrologyCalculations.ComputeCalibrationResults(record, 100.0m, 0.100m);

        // Every point should have distinct air density and Z-factor
        var pt1 = record.Measurements[0];
        var pt2 = record.Measurements[1];
        var pt3 = record.Measurements[2];

        pt1.CalculatedAirDensity.Should().NotBe(pt2.CalculatedAirDensity);
        pt2.CalculatedAirDensity.Should().NotBe(pt3.CalculatedAirDensity);
        pt1.CalculatedAirDensity.Should().NotBe(pt3.CalculatedAirDensity);

        pt1.CalculatedZFactor.Should().NotBe(pt2.CalculatedZFactor);
        pt2.CalculatedZFactor.Should().NotBe(pt3.CalculatedZFactor);
        pt1.CalculatedZFactor.Should().NotBe(pt3.CalculatedZFactor);
    }

    #endregion

    #region 2. Boundary Fallbacks & Robustness

    [Fact]
    public void BoundaryFallbacks_WhenAllPointValuesZero_FallsBackToRecordValues()
    {
        var record = new CalibrationRecordDto
        {
            NominalCapacity = 100.0m,
            MaxPermissibleError = 0.100m,
            Material = VolumetricMaterial.SodaLimeGlass,
            WaterTemperatureCelsius = 21.0m,
            AmbientTemperatureCelsius = 22.0m,
            AtmosphericPressureHpa = 1016.0m,
            RelativeHumidityPercent = 60.0m,
            Measurements = new List<VolumeMeasurementPointDto>
            {
                new()
                {
                    RunNumber = 1,
                    EmptyMassGrams = 50.0m,
                    FullMassGrams = 150.0m,
                    WaterTemperatureCelsius = 0m,
                    AmbientTemperatureCelsius = 0m,
                    AtmosphericPressureHpa = 0m,
                    RelativeHumidityPercent = 0m
                }
            }
        };

        MetrologyCalculations.ComputeCalibrationResults(record, 100.0m, 0.100m);

        var pt = record.Measurements[0];
        var expectedAirDensity = Math.Round(MetrologyCalculations.CalculateAirDensity(1016.0m, 22.0m, 60.0m), 4);
        var expectedWaterDensity = MetrologyCalculations.CalculatePureWaterDensity(21.0m);
        var expectedZ = Math.Round(MetrologyCalculations.CalculateZFactor(expectedWaterDensity, expectedAirDensity, record.Material, 21.0m), 5);

        pt.CalculatedAirDensity.Should().Be(expectedAirDensity);
        pt.CalculatedZFactor.Should().Be(expectedZ);
    }

    [Fact]
    public void BoundaryFallbacks_WhenRecordAmbientValuesZero_FallsBackToStandardConstants()
    {
        var record = new CalibrationRecordDto
        {
            NominalCapacity = 100.0m,
            MaxPermissibleError = 0.100m,
            AmbientTemperatureCelsius = 0m,     // Unset -> should fallback to 20.0°C
            AtmosphericPressureHpa = 0m,        // Unset -> should fallback to 1013.25 hPa
            RelativeHumidityPercent = 0m,       // Unset -> should fallback to 50.0%
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
        var standardAirDensity = Math.Round(MetrologyCalculations.CalculateAirDensity(1013.25m, 20.0m, 50.0m), 4);
        pt.CalculatedAirDensity.Should().Be(standardAirDensity);
    }

    [Fact]
    public void BoundaryFallbacks_EmptyValidPoints_ExitsGracefullyWithoutExceptions()
    {
        var record = new CalibrationRecordDto
        {
            NominalCapacity = 100.0m,
            MaxPermissibleError = 0.100m,
            Measurements = new List<VolumeMeasurementPointDto>
            {
                // FullMass <= EmptyMass -> Invalid point
                new() { RunNumber = 1, EmptyMassGrams = 100m, FullMassGrams = 90m },
                new() { RunNumber = 2, EmptyMassGrams = 100m, FullMassGrams = 100m }
            }
        };

        var act = () => MetrologyCalculations.ComputeCalibrationResults(record, 100.0m, 0.100m);
        act.Should().NotThrow();
        record.MeanV20.Should().Be(0m);
        record.Result.Should().Be(CalibrationResult.Pending);
    }

    [Fact]
    public void BoundaryFallbacks_SinglePoint_ComputesCoverageFactorAndCombinedUncertaintySafely()
    {
        var record = new CalibrationRecordDto
        {
            NominalCapacity = 100.0m,
            MaxPermissibleError = 0.100m,
            WaterTemperatureCelsius = 20.0m,
            Measurements = new List<VolumeMeasurementPointDto>
            {
                new() { RunNumber = 1, EmptyMassGrams = 50.0m, FullMassGrams = 149.8m }
            }
        };

        var act = () => MetrologyCalculations.ComputeCalibrationResults(record, 100.0m, 0.100m);
        act.Should().NotThrow();
        record.SampleStdDev.Should().Be(0m);
        record.CoverageFactor.Should().Be(2.00m);
        record.CombinedUncertainty.Should().BeGreaterThan(0m);
        record.ExpandedUncertainty.Should().BeGreaterThan(0m);
    }

    #endregion

    #region 3. Tanaka vs Manual Water Density

    [Fact]
    public void ManualWaterDensity_OverridesTanakaEvenIfPointHasDifferentWaterTemp()
    {
        // When using a pycnometer or digital densimeter, the measured liquid density is constant
        // for the batch and overrides Tanaka temperature-dependent calculations at each point.
        var record = new CalibrationRecordDto
        {
            NominalCapacity = 100.0m,
            MaxPermissibleError = 0.100m,
            WaterDensityGcm3 = 0.997850m, // Pycnometer experimental density
            PycnometerStandardId = "pad-pic-01",
            BalanceStandardId = "pad-bal-01",
            LiquidThermometerStandardId = "pad-term-01",
            BarometerStandardId = "pad-bar-01",
            ThermoHygrometerStandardId = "pad-hig-01",
            Measurements = new List<VolumeMeasurementPointDto>
            {
                new() { RunNumber = 1, EmptyMassGrams = 50m, FullMassGrams = 150m, WaterTemperatureCelsius = 19.5m },
                new() { RunNumber = 2, EmptyMassGrams = 50m, FullMassGrams = 150m, WaterTemperatureCelsius = 21.0m }
            }
        };

        MetrologyCalculations.ComputeCalibrationResults(record, 100.0m, 0.100m);

        record.CalculatedWaterDensity.Should().Be(0.997850m);
        record.IsManualWaterDensity.Should().BeTrue();

        // Calculate expected Z with manual density
        var pt1 = record.Measurements[0];
        var pt2 = record.Measurements[1];

        var zExpected1 = Math.Round(MetrologyCalculations.CalculateZFactor(0.997850m, pt1.CalculatedAirDensity, record.Material, 19.5m), 5);
        var zExpected2 = Math.Round(MetrologyCalculations.CalculateZFactor(0.997850m, pt2.CalculatedAirDensity, record.Material, 21.0m), 5);

        pt1.CalculatedZFactor.Should().Be(zExpected1);
        pt2.CalculatedZFactor.Should().Be(zExpected2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    public void ManualWaterDensity_WhenNullZeroOrNegative_UsesTanakaPureWaterDensity(double? densityVal)
    {
        var record = new CalibrationRecordDto
        {
            NominalCapacity = 100.0m,
            MaxPermissibleError = 0.100m,
            WaterTemperatureCelsius = 20.0m,
            WaterDensityGcm3 = densityVal.HasValue ? (decimal)densityVal.Value : null,
            Measurements = new List<VolumeMeasurementPointDto>
            {
                new() { RunNumber = 1, EmptyMassGrams = 50m, FullMassGrams = 150m, WaterTemperatureCelsius = 20.0m }
            }
        };

        MetrologyCalculations.ComputeCalibrationResults(record, 100.0m, 0.100m);

        var expectedTanaka = MetrologyCalculations.CalculatePureWaterDensity(20.0m);
        record.CalculatedWaterDensity.Should().Be(expectedTanaka);
        record.IsManualWaterDensity.Should().BeFalse();
        record.RequiresPycnometer.Should().BeFalse();
    }

    #endregion

    #region 4. HasValidStandards Validation Rules (Pycnometer & Multi-Standards)

    [Theory]
    [InlineData("0.998205", null, false)]                // Manual density without pycnometer ID -> INVALID
    [InlineData("0.998205", "", false)]                  // Manual density with empty pycnometer ID -> INVALID
    [InlineData("0.998205", "   ", false)]               // Manual density with whitespace pycnometer ID -> INVALID
    [InlineData("0.998205", "pad-pic-01", true)]         // Manual density with valid pycnometer ID -> VALID
    [InlineData(null, null, true)]                       // Tanaka density without pycnometer ID -> VALID
    [InlineData("0.0", null, true)]                      // Zero density (Tanaka fallback) without pycnometer ID -> VALID
    public void HasValidStandards_EnforcesPycnometerWhenManualDensitySpecified(
        string? manualDensityStr,
        string? pycnometerId,
        bool expectedValid)
    {
        decimal? density = manualDensityStr != null ? decimal.Parse(manualDensityStr) : null;

        var record = new CalibrationRecordDto
        {
            BalanceStandardId = "pad-bal-01",
            LiquidThermometerStandardId = "pad-term-01",
            BarometerStandardId = "pad-bar-01",
            ThermoHygrometerStandardId = "pad-hig-01",
            WaterDensityGcm3 = density,
            PycnometerStandardId = pycnometerId
        };

        record.HasValidStandards().Should().Be(expectedValid);
        if (density.HasValue && density.Value > 0)
        {
            record.RequiresPycnometer.Should().BeTrue();
            record.HasValidPycnometer.Should().Be(!string.IsNullOrWhiteSpace(pycnometerId));
        }
        else
        {
            record.RequiresPycnometer.Should().BeFalse();
            record.HasValidPycnometer.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData("pad-bal-01", "pad-term-01", "pad-bar-01", "pad-hig-01", null, null, true)]           // Combined thermo-hygro
    [InlineData("pad-bal-01", "pad-term-01", "pad-bar-01", null, "pad-teramb-01", "pad-hig-02", true)] // Separate ambient + hygro
    [InlineData("", "pad-term-01", "pad-bar-01", "pad-hig-01", null, null, false)]                    // Missing balance
    [InlineData(null, "pad-term-01", "pad-bar-01", "pad-hig-01", null, null, false)]                  // Null balance
    [InlineData("pad-bal-01", "", "pad-bar-01", "pad-hig-01", null, null, false)]                    // Missing thermometer
    [InlineData("pad-bal-01", "pad-term-01", "", "pad-hig-01", null, null, false)]                    // Missing barometer
    [InlineData("pad-bal-01", "pad-term-01", "pad-bar-01", null, "pad-teramb-01", null, false)]       // Ambient therm without hygrometer
    [InlineData("pad-bal-01", "pad-term-01", "pad-bar-01", null, null, "pad-hig-02", false)]       // Hygrometer without ambient therm
    [InlineData("pad-bal-01", "pad-term-01", "pad-bar-01", null, null, null, false)]                  // No ambient instrumentation
    public void HasValidStandards_MultiStandardsPermutations_EnforcesAllMandatoryStandards(
        string? balId,
        string? termId,
        string? barId,
        string? thermoHygroId,
        string? ambTermId,
        string? hygroId,
        bool expectedValid)
    {
        var record = new CalibrationRecordDto
        {
            BalanceStandardId = balId ?? string.Empty,
            LiquidThermometerStandardId = termId ?? string.Empty,
            BarometerStandardId = barId ?? string.Empty,
            ThermoHygrometerStandardId = thermoHygroId,
            AmbientThermometerStandardId = ambTermId,
            HygrometerStandardId = hygroId
        };

        record.HasValidStandards().Should().Be(expectedValid);
    }

    #endregion

    #region 5. Extreme Environmental Stresses (High Altitude & Climate Chamber)

    [Theory]
    [InlineData(700.0, 15.0, 20.0)]    // High altitude (e.g. La Paz / mountain lab)
    [InlineData(1013.25, 20.0, 50.0)]  // Standard lab
    [InlineData(1060.0, 30.0, 90.0)]   // Tropical high pressure sea level
    [InlineData(900.0, 10.0, 10.0)]    // Cold dry altitude
    public void ExtremeEnvironmentalStresses_CalculatesPhysicalAirDensity(
        double pressure,
        double temp,
        double humidity)
    {
        var airDensity = MetrologyCalculations.CalculateAirDensity(
            (decimal)pressure,
            (decimal)temp,
            (decimal)humidity);

        // Under realistic terrestrial conditions, air density is between 0.7 kg/m³ and 1.5 kg/m³
        airDensity.Should().BeInRange(0.70m, 1.50m);
    }

    [Fact]
    public void ExtremeEnvironmentalStresses_ExtremeNegativeTemperature_SpieweckSafelyHandlesSingularity()
    {
        // Absolute zero limit (den <= 0) should return default 1.200 kg/m³ rather than throwing
        var airDensity = MetrologyCalculations.CalculateAirDensity(1013.25m, -273.15m, 50.0m);
        airDensity.Should().Be(1.200m);

        var airDensityDeep = MetrologyCalculations.CalculateAirDensity(1013.25m, -300.0m, 50.0m);
        airDensityDeep.Should().Be(1.200m);
    }

    #endregion

    #region 6. Standards Lifecycle & Pycnometer Expiry Stress Tests

    [Theory]
    [InlineData(0, true, "Vencimento Crítico")]
    [InlineData(7, true, "Vencimento Crítico")]
    [InlineData(8, true, "Alerta de Vencimento")]
    [InlineData(30, true, "Alerta de Vencimento")]
    [InlineData(31, true, "Vigente")]
    [InlineData(-1, false, "Expirado / Vencido")]
    [InlineData(-10, false, "Expirado / Vencido")]
    public void StandardCertificateDto_StatusAndValidity_ComputesCorrectlyAcrossBoundaries(
        int daysOffset,
        bool expectedValid,
        string expectedStatus)
    {
        var cert = new StandardCertificateDto
        {
            CalibrationDate = DateTime.UtcNow.AddMonths(-12),
            CalibrationDueDate = DateTime.UtcNow.Date.AddDays(daysOffset),
            IsActive = true
        };

        cert.IsValid.Should().Be(expectedValid);
        cert.Status.Should().Be(expectedStatus);
    }

    [Fact]
    public void StandardCertificateDto_InactiveCertificate_ReturnsSubstitutedStatusRegardlessOfDate()
    {
        var cert = new StandardCertificateDto
        {
            CalibrationDueDate = DateTime.UtcNow.AddYears(1),
            IsActive = false
        };

        cert.Status.Should().Be("Substituído / Histórico");
    }

    [Fact]
    public async Task StandardsValidation_DistinguishesValidPycnometerFromExpiredPycnometer()
    {
        var service = new MockMetrologyDataService();
        var pycnometers = await service.GetStandardEquipmentAsync(role: StandardRole.Pycnometer);

        var validPyc = pycnometers.FirstOrDefault(p => p.Id == "pad-pic-01");
        var expiredPyc = pycnometers.FirstOrDefault(p => p.Id == "pad-pic-02");

        validPyc.Should().NotBeNull();
        validPyc!.IsValid.Should().BeTrue();
        validPyc.DaysUntilExpiry.Should().BeGreaterThan(0);

        expiredPyc.Should().NotBeNull();
        expiredPyc!.IsValid.Should().BeFalse();
        expiredPyc.DaysUntilExpiry.Should().BeLessThan(0);
    }

    [Fact]
    public async Task StandardsLifecycle_RobustInputValidation_EnforcesMetrologicalInvariants()
    {
        var service = new MockMetrologyDataService();

        // 1. Unknown standard throws KeyNotFoundException
        var unknownGuid = Guid.NewGuid();
        var actUnknown = () => service.SendStandardToCalibrationAsync(unknownGuid, "IPT", DateTime.UtcNow.AddDays(30));
        await actUnknown.Should().ThrowAsync<KeyNotFoundException>();

        // 2. Due date prior to calibration date throws ArgumentException
        var invalidCert = new StandardCertificateDto
        {
            CertificateNumber = "CAL-INV-001",
            CalibratingLaboratory = "IPT",
            CalibrationDate = DateTime.UtcNow,
            CalibrationDueDate = DateTime.UtcNow.AddDays(-1) // Invalid!
        };
        var actInvalidCert = () => service.RegisterStandardCertificateAsync(MockMetrologyDataService.StandardBalanceId, invalidCert);
        await actInvalidCert.Should().ThrowAsync<ArgumentException>();

        // 3. Update standard with inverted range throws ArgumentException
        var badStandard = new StandardEquipmentDto
        {
            Id = "pad-bal-01",
            LabTag = "PAD-BAL-01",
            RangeMin = 220.0m,
            RangeMax = 0.001m // Inverted!
        };
        var actBadRange = () => service.UpdateStandardAsync(badStandard);
        await actBadRange.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
