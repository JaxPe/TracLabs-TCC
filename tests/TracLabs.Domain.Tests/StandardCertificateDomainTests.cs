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
    public void StandardCertificate_WithHysteresisPoints_ShouldConsolidateAndInterpolateWithoutDivisionByZero()
    {
        // Arrange - Ensaio com ciclo fechado (histerese): medições em subida e descida
        var points = new List<StandardCalibrationPoint>
        {
            new(1, 10.0m, 0m, 0.04m, 0.02m, 2.0m),
            new(2, 10.0m, 0m, 0.06m, 0.03m, 2.0m), // Mesma indicação 10.0 (histerese): média 0.05, max U = 0.03
            new(3, 50.0m, 0m, 0.14m, 0.02m, 2.0m),
            new(4, 50.0m, 0m, 0.16m, 0.04m, 2.0m), // Média 0.15, max U = 0.04
            new(5, 100.0m, 0m, 0.24m, 0.02m, 2.0m),
            new(6, 100.0m, 0m, 0.26m, 0.05m, 2.0m)  // Média 0.25, max U = 0.05
        };

        var cert = new StandardCertificate(
            standardEquipmentId: Guid.NewGuid(),
            certificateNumber: "CERT-HYSTERESIS-001",
            calibratingLaboratory: "Laboratório RBC",
            calibrationDate: DateTime.UtcNow.AddMonths(-1),
            calibrationDueDate: DateTime.UtcNow.AddMonths(11),
            points: points,
            fitType: CurveFitType.PiecewiseLinear,
            uncertaintyPolicy: UncertaintyPolicy.Interpolated);

        // Act - Ponto exato 10.0 e ponto intermediário 30.0 (meio do caminho entre 10 e 50: 0.05 + 0.5*(0.15 - 0.05) = 0.10)
        var resolvedAt10 = cert.ResolveCorrectionAt(10.0m);
        var resolvedAt30 = cert.ResolveCorrectionAt(30.0m);

        // Assert
        resolvedAt10.CalculatedCorrection.Should().Be(0.05m);
        resolvedAt10.CalculatedUncertainty.Should().Be(0.03m); // Max U no ponto 10

        resolvedAt30.CalculatedCorrection.Should().Be(0.10m);
        resolvedAt30.CalculatedUncertainty.Should().Be(0.035m); // 0.03 + 0.5*(0.04 - 0.03) = 0.035
        resolvedAt30.IsExtrapolated.Should().BeFalse();
    }

    [Fact]
    public void StandardCertificate_WithVerticalPoints_ShouldSetSlopeAndR2ToNull()
    {
        // Arrange - Conjunto de pontos colineares verticais (todos no mesmo X)
        var points = new List<StandardCalibrationPoint>
        {
            new(1, 20.0m, 0m, 0.1m, 0.02m, 2.0m),
            new(2, 20.0m, 0m, 0.2m, 0.02m, 2.0m),
            new(3, 20.0m, 0m, 0.3m, 0.02m, 2.0m)
        };

        // Act
        var cert = new StandardCertificate(
            standardEquipmentId: Guid.NewGuid(),
            certificateNumber: "CERT-VERTICAL",
            calibratingLaboratory: "Laboratório RBC",
            calibrationDate: DateTime.UtcNow.AddMonths(-1),
            calibrationDueDate: DateTime.UtcNow.AddMonths(11),
            points: points,
            fitType: CurveFitType.LinearRegression);

        // Assert - Reta vertical possui inclinação indefinida
        cert.Slope.Should().BeNull();
        cert.Intercept.Should().BeNull();
        cert.R2.Should().BeNull();
    }

    [Fact]
    public void StandardCertificate_ExtrapolationPolicy_ShouldDifferentiateLinearFromPiecewise()
    {
        // Arrange - Certificado com escopo formal de 0 a 100, mas pontos calibrados de 20 a 80
        var points = new List<StandardCalibrationPoint>
        {
            new(1, 20.0m, 0m, 0.02m, 0.01m, 2.0m),
            new(2, 50.0m, 0m, 0.05m, 0.01m, 2.0m),
            new(3, 80.0m, 0m, 0.08m, 0.01m, 2.0m)
        };

        var certLinear = new StandardCertificate(
            standardEquipmentId: Guid.NewGuid(),
            certificateNumber: "CERT-SCOPE",
            calibratingLaboratory: "Lab RBC",
            calibrationDate: DateTime.UtcNow.AddMonths(-1),
            calibrationDueDate: DateTime.UtcNow.AddMonths(11),
            calibrationRangeMin: 0.0m,
            calibrationRangeMax: 100.0m,
            fitType: CurveFitType.LinearRegression);
        certLinear.SetPoints(points);

        var certPiecewise = new StandardCertificate(
            standardEquipmentId: Guid.NewGuid(),
            certificateNumber: "CERT-SCOPE",
            calibratingLaboratory: "Lab RBC",
            calibrationDate: DateTime.UtcNow.AddMonths(-1),
            calibrationDueDate: DateTime.UtcNow.AddMonths(11),
            calibrationRangeMin: 0.0m,
            calibrationRangeMax: 100.0m,
            fitType: CurveFitType.PiecewiseLinear);
        certPiecewise.SetPoints(points);

        // Act - Ponto 10.0m (dentro da faixa declarada 0-100, mas fora dos pontos 20-80)
        var resLinear = certLinear.ResolveCorrectionAt(10.0m);
        var resPiecewise = certPiecewise.ResolveCorrectionAt(10.0m);

        // Assert:
        // Na regressão linear OLS: reta global é válida em toda a faixa formal acreditada 0 a 100
        resLinear.IsExtrapolated.Should().BeFalse();
        resLinear.WarningMessage.Should().BeNull();

        // Na interpolação segmentada: além de 20-80 é extrapolação cega (perda de rastreabilidade)
        resPiecewise.IsExtrapolated.Should().BeTrue();
        resPiecewise.WarningMessage.Should().Contain("extrapola a faixa dos pontos medidos");
    }

    [Fact]
    public void CalibrationStandardUsed_Correction_ShouldMatchStandardCorrectionSign()
    {
        // Arrange
        var standard = new StandardEquipment(
            labTag: "PAD-CORR-SIGN",
            model: "M1",
            manufacturer: "Mettler",
            serialNumber: "SN-999",
            instrumentType: "Balança",
            error: 0.005m,
            expandedUncertainty: 0.002m,
            coverageFactor: 2.0m,
            lastCalibrationDate: DateTime.UtcNow.AddMonths(-2),
            calibrationDueDate: DateTime.UtcNow.AddMonths(10),
            certificateNumber: "CERT-SIGN",
            calibratingLaboratory: "Lab RBC",
            resolution: 0.001m);

        // standard.Error = 0.005, portanto standard.Correction = -0.005
        standard.Error.Should().Be(0.005m);
        standard.Correction.Should().Be(-0.005m);

        // Act - Construtor snapshot da entidade CalibrationStandardUsed
        var snapshot = new CalibrationStandardUsed(Guid.NewGuid(), standard, StandardRole.Balance);

        // Assert - A correção congelada DEVE ter sinal invertido em relação ao erro (Correction = -Error)
        snapshot.Correction.Should().Be(-0.005m);
        snapshot.Correction.Should().NotBe(snapshot.Correction * -1);
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
