using FluentAssertions;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;
using Xunit;

namespace TracLabs.Domain.Tests;

public class StandardCertificateDomainTests
{
    [Fact]
    public void StandardCertificate_WithPoints_ShouldAutomaticallyCalculateRegressionCoefficients()
    {
        // Arrange
        var points = new List<StandardCalibrationPoint>
        {
            new(1, 19.0m, -0.1m, 0.1m, 0.1m, 2.0m),
            new(2, 20.0m, -0.2m, 0.2m, 0.1m, 2.0m),
            new(3, 21.0m, -0.3m, 0.3m, 0.1m, 2.0m)
        };

        // Act
        var cert = new StandardCertificate(
            standardEquipmentId: Guid.NewGuid(),
            certificateNumber: "CERT-RBC-2026-001",
            calibratingLaboratory: "Laboratório Metrológico Acreditado RBC",
            calibrationDate: DateTime.UtcNow.AddMonths(-1),
            calibrationDueDate: DateTime.UtcNow.AddMonths(11),
            points: points,
            fitType: CurveFitType.LinearRegression,
            uncertaintyPolicy: UncertaintyPolicy.WorstCase);

        // Assert - Excel formula: y = 0.1x - 1.8, R² = 1
        cert.Slope.Should().Be(0.1m);
        cert.Intercept.Should().Be(-1.8m);
        cert.R2.Should().Be(1.0m);
        cert.CalibrationRangeMin.Should().Be(19.0m);
        cert.CalibrationRangeMax.Should().Be(21.0m);
        cert.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ResolveCorrectionAt_WhenWithinRange_ShouldReturnCalculatedValuesWithoutWarning()
    {
        // Arrange
        var points = new List<StandardCalibrationPoint>
        {
            new(1, 19.0m, -0.1m, 0.1m, 0.1m, 2.0m),
            new(2, 20.0m, -0.2m, 0.2m, 0.1m, 2.0m),
            new(3, 21.0m, -0.3m, 0.3m, 0.1m, 2.0m)
        };

        var cert = new StandardCertificate(
            standardEquipmentId: Guid.NewGuid(),
            certificateNumber: "CERT-RBC-2026-001",
            calibratingLaboratory: "Laboratório RBC",
            calibrationDate: DateTime.UtcNow.AddMonths(-1),
            calibrationDueDate: DateTime.UtcNow.AddMonths(11),
            points: points);

        // Act - Ponto 20.2°C
        var resolved = cert.ResolveCorrectionAt(20.2m);

        // Assert: 0.1 * 20.2 - 1.8 = 0.22
        resolved.CalculatedCorrection.Should().Be(0.22m);
        resolved.CalculatedUncertainty.Should().Be(0.1m);
        resolved.IsExtrapolated.Should().BeFalse();
        resolved.WarningMessage.Should().BeNull();
    }

    [Fact]
    public void ResolveCorrectionAt_WhenOutsideCalibrationRange_ShouldFlagExtrapolatedAndSetWarning()
    {
        // Arrange
        var points = new List<StandardCalibrationPoint>
        {
            new(1, 19.0m, -0.1m, 0.1m, 0.1m, 2.0m),
            new(2, 20.0m, -0.2m, 0.2m, 0.1m, 2.0m),
            new(3, 21.0m, -0.3m, 0.3m, 0.1m, 2.0m)
        };

        var cert = new StandardCertificate(
            standardEquipmentId: Guid.NewGuid(),
            certificateNumber: "CERT-RBC-2026-001",
            calibratingLaboratory: "Laboratório RBC",
            calibrationDate: DateTime.UtcNow.AddMonths(-1),
            calibrationDueDate: DateTime.UtcNow.AddMonths(11),
            points: points);

        // Act - Ponto 25.0°C (fora do range 19 a 21)
        var resolved = cert.ResolveCorrectionAt(25.0m);

        // Assert
        resolved.IsExtrapolated.Should().BeTrue();
        resolved.WarningMessage.Should().NotBeNullOrWhiteSpace();
        resolved.WarningMessage.Should().Contain("CERT-RBC-2026-001");
        resolved.WarningMessage.Should().Contain("19");
        resolved.WarningMessage.Should().Contain("21");
    }

    [Fact]
    public void StandardEquipment_AddCertificate_ShouldDeactivatePriorAndActivateNewCertificate()
    {
        // Arrange
        var standard = new StandardEquipment(
            labTag: "PAD-TERMO-01",
            model: "F200",
            manufacturer: "Fluke",
            serialNumber: "SN-987654",
            instrumentType: "Termômetro Digital",
            rangeMin: 0m,
            rangeMax: 100m,
            resolution: 0.01m,
            currentLocation: "Bancada 1");

        var cert1 = new StandardCertificate(
            standardEquipmentId: standard.Id,
            certificateNumber: "CERT-2025",
            calibratingLaboratory: "Lab Antigo",
            calibrationDate: DateTime.UtcNow.AddYears(-1),
            calibrationDueDate: DateTime.UtcNow.AddDays(-10),
            points: new List<StandardCalibrationPoint>
            {
                new(1, 20.0m, 0m, 0.1m, 0.05m, 2.0m)
            });

        standard.AddCertificate(cert1);
        standard.GetActiveCertificate().Should().Be(cert1);
        cert1.IsActive.Should().BeTrue();

        // Act - Adiciona novo certificado de calibração periódica (Etapa 2)
        var cert2 = new StandardCertificate(
            standardEquipmentId: standard.Id,
            certificateNumber: "CERT-2026",
            calibratingLaboratory: "Lab Novo RBC",
            calibrationDate: DateTime.UtcNow,
            calibrationDueDate: DateTime.UtcNow.AddYears(1),
            points: new List<StandardCalibrationPoint>
            {
                new(1, 19.0m, -0.1m, 0.1m, 0.04m, 2.0m),
                new(2, 21.0m, -0.3m, 0.3m, 0.04m, 2.0m)
            });

        standard.AddCertificate(cert2);

        // Assert
        cert1.IsActive.Should().BeFalse();
        cert2.IsActive.Should().BeTrue();
        standard.GetActiveCertificate().Should().Be(cert2);
        standard.CalibrationCertificateNumber.Should().Be("CERT-2026");
        standard.CalibratingLaboratory.Should().Be("Lab Novo RBC");
        standard.CalibrationRangeMin.Should().Be(19.0m);
        standard.CalibrationRangeMax.Should().Be(21.0m);
        standard.Status.Should().Be(StandardEquipmentStatus.Calibrated);
    }

    [Fact]
    public void Equipment_CalibrationRange_ShouldCheckProperly()
    {
        // Arrange
        var equip = new Equipment(
            labTag: "PIP-1000",
            model: "Research Plus",
            manufacturer: "Eppendorf",
            serialNumber: "EP-12345",
            instrumentType: "Micropipeta Monocanal",
            rangeMin: 100m,
            rangeMax: 1000m,
            unit: "µL",
            resolution: 1m,
            clientId: Guid.NewGuid(),
            calibrationRangeMin: 200m,
            calibrationRangeMax: 1000m);

        // Assert
        equip.IsWithinCalibrationRange(500m).Should().BeTrue();
        equip.IsWithinCalibrationRange(150m).Should().BeFalse();
        equip.IsWithinCalibrationRange(1100m).Should().BeFalse();
    }
}
