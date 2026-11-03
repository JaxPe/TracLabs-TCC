using FluentAssertions;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;
using Xunit;

namespace TracLabs.Domain.Tests;

public class CalibrationStandardsUsedDomainTests
{
    private StandardEquipment CreateStandard(string tag, StandardRole role)
    {
        return new StandardEquipment(
            labTag: tag,
            model: "StandardModel",
            manufacturer: "Precision Corp",
            serialNumber: $"SN-{tag}",
            instrumentType: role.ToString(),
            error: 0.002m,
            expandedUncertainty: 0.005m,
            coverageFactor: 2.05m,
            lastCalibrationDate: DateTime.UtcNow.AddMonths(-2),
            calibrationDueDate: DateTime.UtcNow.AddMonths(10),
            certificateNumber: $"CERT-{tag}-2026",
            calibratingLaboratory: "Inmetro RBC Lab",
            resolution: 0.001m,
            status: StandardEquipmentStatus.Calibrated);
    }

    [Fact]
    public void CalibrationStandardUsed_ShouldSnapshotStandardEquipmentState()
    {
        // Arrange
        var calibrationId = Guid.NewGuid();
        var standard = CreateStandard("PAD-BAL-01", StandardRole.Balance);

        // Act
        var snapshot = new CalibrationStandardUsed(calibrationId, standard, StandardRole.Balance);

        // Assert
        snapshot.CalibrationId.Should().Be(calibrationId);
        snapshot.StandardEquipmentId.Should().Be(standard.Id);
        snapshot.Role.Should().Be(StandardRole.Balance);
        snapshot.LabTag.Should().Be(standard.LabTag);
        snapshot.SerialNumber.Should().Be(standard.SerialNumber);
        snapshot.CertificateNumber.Should().Be(standard.CalibrationCertificateNumber);
        snapshot.Resolution.Should().Be(standard.Resolution);
        snapshot.Correction.Should().Be(standard.Correction);
        snapshot.Correction.Should().Be(-standard.Error);
        snapshot.ExpandedUncertainty.Should().Be(standard.ExpandedUncertainty);
        snapshot.CoverageFactor.Should().Be(standard.CoverageFactor);
    }

    [Fact]
    public void CalibrationRecord_AddStandardsUsed_ShouldAssociateMultipleStandards()
    {
        // Arrange
        var equipmentId = Guid.NewGuid();
        var balance = CreateStandard("PAD-BAL-01", StandardRole.Balance);
        var liquidTherm = CreateStandard("PAD-TER-LIQ", StandardRole.LiquidThermometer);
        var barometer = CreateStandard("PAD-BAR-01", StandardRole.Barometer);
        var hygrometer = CreateStandard("PAD-HIG-01", StandardRole.Hygrometer);

        var record = new CalibrationRecord(
            equipmentId,
            balance.Id,
            "tech-1",
            "Técnico de Calibração",
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(1),
            20.1m,
            55.0m);

        var standards = new List<CalibrationStandardUsed>
        {
            new(record.Id, balance, StandardRole.Balance),
            new(record.Id, liquidTherm, StandardRole.LiquidThermometer),
            new(record.Id, barometer, StandardRole.Barometer),
            new(record.Id, hygrometer, StandardRole.Hygrometer)
        };

        // Act
        record.AddStandardsUsed(standards);

        // Assert
        record.StandardsUsed.Should().HaveCount(4);
        record.StandardsUsed.Select(s => s.Role).Should().Contain(new[]
        {
            StandardRole.Balance,
            StandardRole.LiquidThermometer,
            StandardRole.Barometer,
            StandardRole.Hygrometer
        });
    }

    [Fact]
    public void VolumeGravimetricPoint_ShouldStorePerPointAmbientConditions()
    {
        // Arrange & Act
        var point = new VolumeGravimetricPoint(
            runNumber: 1,
            fullMassGrams: 150.250m,
            emptyMassGrams: 50.120m,
            waterTemperatureCelsius: 20.05m,
            calculatedVolumeMl: 100.231m,
            ambientTemperatureCelsius: 20.30m,
            atmosphericPressureHpa: 1013.25m,
            relativeHumidityPercent: 55.40m,
            calculatedAirDensityKgPerM3: 1.1982m);

        // Assert
        point.RunNumber.Should().Be(1);
        point.FullMassGrams.Should().Be(150.250m);
        point.EmptyMassGrams.Should().Be(50.120m);
        point.WaterTemperatureCelsius.Should().Be(20.05m);
        point.CalculatedVolumeMl.Should().Be(100.231m);
        point.AmbientTemperatureCelsius.Should().Be(20.30m);
        point.AtmosphericPressureHpa.Should().Be(1013.25m);
        point.RelativeHumidityPercent.Should().Be(55.40m);
        point.CalculatedAirDensityKgPerM3.Should().Be(1.1982m);
    }
}
