using FluentAssertions;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using Xunit;

namespace TracLabs.Domain.Tests;

public class StandardEquipmentTests
{
    private StandardEquipment CreateValidStandard(
        DateTime? lastCalDate = null,
        DateTime? dueDate = null,
        StandardEquipmentStatus status = StandardEquipmentStatus.Calibrated)
    {
        var last = lastCalDate ?? DateTime.UtcNow.AddMonths(-6);
        var due = dueDate ?? DateTime.UtcNow.AddMonths(6);

        return new StandardEquipment(
            labTag: "PAD-VOL-001",
            model: "XP205",
            manufacturer: "Mettler Toledo",
            serialNumber: "SN123456",
            instrumentType: "Balança Analítica",
            error: 0.0001m,
            expandedUncertainty: 0.0002m,
            coverageFactor: 2.0m,
            lastCalibrationDate: last,
            calibrationDueDate: due,
            certificateNumber: "CAL-2026-999",
            calibratingLaboratory: "Laboratório Metrológico RBC",
            resolution: 0.00001m,
            name: "Balança Analítica Padrão",
            softwareVersion: null, // opcional para analógicos / vidrarias / instrumentos sem software
            firmwareVersion: "v2.1.0",
            currentLocation: "Laboratório de Massa e Volume - Bancada 01",
            acceptanceCriteria: "EMA <= 0,0005 g; U <= 0,0002 g",
            calibrationIntervalMonths: 12,
            maintenancePlan: "Verificação intermediária semestral e manutenção preventiva anual",
            status: status);
    }

    [Fact]
    public void Constructor_WithValidArguments_ShouldInstantiateCorrectly()
    {
        // Act
        var standard = CreateValidStandard();

        // Assert
        standard.LabTag.Should().Be("PAD-VOL-001");
        standard.Name.Should().Be("Balança Analítica Padrão");
        standard.SoftwareVersion.Should().BeNull();
        standard.FirmwareVersion.Should().Be("v2.1.0");
        standard.CurrentLocation.Should().Be("Laboratório de Massa e Volume - Bancada 01");
        standard.Status.Should().Be(StandardEquipmentStatus.Calibrated);
        standard.IsCalibrationValid.Should().BeTrue();
        standard.IsUsableForCalibration.Should().BeTrue();
    }

    [Fact]
    public void SendToCalibration_ShouldChangeStatusToInCalibrationAndBlockUsage()
    {
        // Arrange
        var standard = CreateValidStandard();

        // Act
        standard.SendToCalibration("Instituto Nacional de Metrologia", "Calibração periódica");

        // Assert
        standard.Status.Should().Be(StandardEquipmentStatus.InCalibration);
        standard.CalibratingLaboratory.Should().Be("Instituto Nacional de Metrologia");
        standard.IsUsableForCalibration.Should().BeFalse();
        standard.MaintenanceHistory.Should().ContainSingle(m => m.Type == MaintenanceType.CalibrationAdjustment);
    }

    [Fact]
    public void SendToCalibration_WhenAlreadyInCalibration_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var standard = CreateValidStandard(status: StandardEquipmentStatus.InCalibration);

        // Act
        var act = () => standard.SendToCalibration("Laboratório Externo");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*já está em processo de calibração*");
    }

    [Fact]
    public void RegisterCalibrationResult_WhenMeetsAcceptanceCriteria_ShouldSetStatusToCalibrated()
    {
        // Arrange
        var standard = CreateValidStandard(status: StandardEquipmentStatus.InCalibration);
        var calDate = DateTime.UtcNow;
        var newDueDate = calDate.AddYears(1);

        // Act
        standard.RegisterCalibrationResult(
            newError: 0.00008m,
            newExpandedUncertainty: 0.00015m,
            coverageFactor: 2.0m,
            calibrationDate: calDate,
            dueDate: newDueDate,
            newCertificateNumber: "CERT-RBC-2026-0042",
            calibratingLaboratory: "Inmetro",
            newResolution: 0.00001m,
            meetsAcceptanceCriteria: true);

        // Assert
        standard.Status.Should().Be(StandardEquipmentStatus.Calibrated);
        standard.Error.Should().Be(0.00008m);
        standard.Correction.Should().Be(-0.00008m);
        standard.CalibrationCertificateNumber.Should().Be("CERT-RBC-2026-0042");
        standard.IsUsableForCalibration.Should().BeTrue();
    }

    [Fact]
    public void RegisterCalibrationResult_WhenFailsAcceptanceCriteria_ShouldSetStatusToOutOfServiceAndLogOccurrence()
    {
        // Arrange
        var standard = CreateValidStandard(status: StandardEquipmentStatus.InCalibration);
        var calDate = DateTime.UtcNow;
        var newDueDate = calDate.AddYears(1);

        // Act
        standard.RegisterCalibrationResult(
            newError: 0.005m, // erro reprovado
            newExpandedUncertainty: 0.002m,
            coverageFactor: 2.0m,
            calibrationDate: calDate,
            dueDate: newDueDate,
            newCertificateNumber: "CERT-RBC-2026-FAIL",
            calibratingLaboratory: "Inmetro",
            meetsAcceptanceCriteria: false);

        // Assert
        standard.Status.Should().Be(StandardEquipmentStatus.OutOfService);
        standard.IsUsableForCalibration.Should().BeFalse();
        standard.OccurrenceHistory.Should().ContainSingle(o => o.Type == OccurrenceType.Malfunction);
    }

    [Fact]
    public void PutOutOfService_ShouldSetStatusToOutOfServiceAndRecordOccurrence()
    {
        // Arrange
        var standard = CreateValidStandard();

        // Act
        standard.PutOutOfService("Queda de objeto sobre o prato da balança", "Técnico Metrologista");

        // Assert
        standard.Status.Should().Be(StandardEquipmentStatus.OutOfService);
        standard.IsUsableForCalibration.Should().BeFalse();
        standard.OccurrenceHistory.Should().ContainSingle(o =>
            o.Type == OccurrenceType.Damage &&
            o.Description.Contains("Queda de objeto"));
    }

    [Fact]
    public void CheckCalibrationValidity_WhenDatePastDueDate_ShouldChangeStatusToPendingCalibration()
    {
        // Arrange — vencido há 2 dias
        var calDate = DateTime.UtcNow.AddMonths(-13);
        var dueDate = DateTime.UtcNow.AddDays(-2);
        var standard = CreateValidStandard(lastCalDate: calDate, dueDate: dueDate, status: StandardEquipmentStatus.Calibrated);

        // Act
        standard.CheckCalibrationValidity();

        // Assert
        standard.Status.Should().Be(StandardEquipmentStatus.PendingCalibration);
        standard.IsUsableForCalibration.Should().BeFalse();
    }

    [Fact]
    public void Reactivate_WhenCalibrationIsValid_ShouldReturnToCalibrated()
    {
        // Arrange
        var standard = CreateValidStandard();
        standard.PutOutOfService("Aguardando verificação", "Gestor");

        // Act
        standard.Reactivate("Verificação intermediária realizada com sucesso", "Responsável Técnico");

        // Assert
        standard.Status.Should().Be(StandardEquipmentStatus.Calibrated);
        standard.IsUsableForCalibration.Should().BeTrue();
        standard.MaintenanceHistory.Should().ContainSingle(m => m.Type == MaintenanceType.Preventive);
    }
}
