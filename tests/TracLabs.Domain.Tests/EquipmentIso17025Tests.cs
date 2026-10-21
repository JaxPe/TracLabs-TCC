using FluentAssertions;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using Xunit;

namespace TracLabs.Domain.Tests;

public class EquipmentIso17025Tests
{
    private Equipment CreateClientEquipment()
    {
        return new Equipment(
            labTag: "EQ-CLI-001",
            model: "Mitutoyo 500-196-30",
            manufacturer: "Mitutoyo",
            serialNumber: "SN987654",
            instrumentType: "Paquímetro Digital",
            rangeMin: 0m,
            rangeMax: 150m,
            unit: "mm",
            resolution: 0.01m,
            clientId: Guid.NewGuid(),
            name: "Paquímetro Digital 150mm",
            softwareVersion: null,
            firmwareVersion: "v1.0",
            currentLocation: "Bancada Dimensional 01",
            acceptanceCriteria: "Erro máximo permissível: ± 0,02 mm",
            maintenancePlan: "Limpeza das faces de medição e lubrificação",
            initialConditionNotes: "Leve risco na carcaça plástica, faces de medição limpas.");
    }

    private StandardEquipment CreateUsableStandard()
    {
        return new StandardEquipment(
            labTag: "PAD-BLC-001",
            model: "Grau 0",
            manufacturer: "Mitutoyo",
            serialNumber: "SN-BLOCKS-01",
            instrumentType: "Jogo de Blocos Padrão",
            error: 0.00005m,
            expandedUncertainty: 0.0001m,
            coverageFactor: 2.0m,
            lastCalibrationDate: DateTime.UtcNow.AddMonths(-3),
            calibrationDueDate: DateTime.UtcNow.AddMonths(9),
            certificateNumber: "CAL-BLOCOS-2026",
            calibratingLaboratory: "Lab RBC Credenciado",
            resolution: 0.00001m,
            name: "Jogo de Blocos Padrão Aço",
            softwareVersion: null, // mecânico / vidraria / bloco -> opcional nulo
            firmwareVersion: null,
            currentLocation: "Sala Climatizada - Armário Padrões",
            acceptanceCriteria: "ISO 3650 Grau 0",
            calibrationIntervalMonths: 12);
    }

    [Fact]
    public void Equipment_ISO17025_Records_ShouldBePreservedAndRetrievable()
    {
        // Arrange
        var equipment = CreateClientEquipment();

        // Act - Registro de manutenção (6.4.13 g)
        var maint = equipment.AddMaintenanceRecord(
            DateTime.UtcNow,
            MaintenanceType.CleaningOrDecontamination,
            "Limpeza ultrassônica das faces de medição",
            "Técnico de Triagem",
            "Faces desengraxadas com álcool isopropílico.");

        // Act - Registro de avaria / dano (6.4.13 h)
        var occ = equipment.ReportOccurrence(
            DateTime.UtcNow,
            OccurrenceType.Damage,
            "Risco superficial constatado no bico interno superior",
            "Fotografado e notificado ao cliente antes do início da calibração",
            "Recebimento",
            resolvedDate: DateTime.UtcNow);

        // Assert
        equipment.Name.Should().Be("Paquímetro Digital 150mm");
        equipment.CurrentLocation.Should().Be("Bancada Dimensional 01");
        equipment.AcceptanceCriteria.Should().Be("Erro máximo permissível: ± 0,02 mm");
        equipment.InitialConditionNotes.Should().Contain("Leve risco");
        equipment.MaintenanceHistory.Should().ContainSingle().Which.Should().Be(maint);
        equipment.OccurrenceHistory.Should().ContainSingle().Which.Should().Be(occ);
    }

    [Fact]
    public void CalibrationRecord_WithUsableStandard_ShouldSucceed()
    {
        // Arrange
        var clientEquipment = CreateClientEquipment();
        var standard = CreateUsableStandard();

        // Act
        var record = new CalibrationRecord(
            clientEquipment,
            standard,
            technicianId: "tech-123",
            technicianName: "Maria Santos",
            calibrationDate: DateTime.UtcNow,
            nextCalibrationDue: DateTime.UtcNow.AddYears(1),
            temperatureCelsius: 20.0m,
            humidityPercent: 55.0m);

        // Assert
        record.StandardEquipmentId.Should().Be(standard.Id);
        record.EquipmentId.Should().Be(clientEquipment.Id);
    }

    [Fact]
    public void CalibrationRecord_WithStandardInCalibration_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var clientEquipment = CreateClientEquipment();
        var standard = CreateUsableStandard();
        standard.SendToCalibration("Laboratório Externo RBC");

        // Act
        var act = () => new CalibrationRecord(
            clientEquipment,
            standard,
            technicianId: "tech-123",
            technicianName: "Maria Santos",
            calibrationDate: DateTime.UtcNow,
            nextCalibrationDue: DateTime.UtcNow.AddYears(1),
            temperatureCelsius: 20.0m,
            humidityPercent: 55.0m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*não pode ser utilizado para calibração*InCalibration*");
    }

    [Fact]
    public void CalibrationRecord_WithStandardOutOfService_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var clientEquipment = CreateClientEquipment();
        var standard = CreateUsableStandard();
        standard.PutOutOfService("Danificado durante transporte", "Auditor");

        // Act
        var act = () => new CalibrationRecord(
            clientEquipment,
            standard,
            technicianId: "tech-123",
            technicianName: "Maria Santos",
            calibrationDate: DateTime.UtcNow,
            nextCalibrationDue: DateTime.UtcNow.AddYears(1),
            temperatureCelsius: 20.0m,
            humidityPercent: 55.0m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*não pode ser utilizado para calibração*OutOfService*");
    }

    [Fact]
    public void CalibrationRecord_WithExpiredStandard_ShouldThrowInvalidOperationException()
    {
        // Arrange — standard vencido
        var clientEquipment = CreateClientEquipment();
        var expiredStandard = new StandardEquipment(
            labTag: "PAD-EXP-001",
            model: "M1",
            manufacturer: "Inmetro",
            serialNumber: "SN-EXP-01",
            instrumentType: "Peso Padrão",
            error: 0m,
            expandedUncertainty: 0.001m,
            coverageFactor: 2m,
            lastCalibrationDate: DateTime.UtcNow.AddMonths(-24),
            calibrationDueDate: DateTime.UtcNow.AddDays(-1), // expirado ontem
            certificateNumber: "CAL-VENCIDO",
            calibratingLaboratory: "Lab");

        // Act
        var act = () => new CalibrationRecord(
            clientEquipment,
            expiredStandard,
            technicianId: "tech-123",
            technicianName: "Maria Santos",
            calibrationDate: DateTime.UtcNow,
            nextCalibrationDue: DateTime.UtcNow.AddYears(1),
            temperatureCelsius: 20.0m,
            humidityPercent: 55.0m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*não pode ser utilizado para calibração*");
    }
}
