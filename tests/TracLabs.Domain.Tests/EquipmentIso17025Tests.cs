using FluentAssertions;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using Xunit;

namespace TracLabs.Domain.Tests;

/// <summary>
/// Testes de validação dos requisitos do item 6.4.13 da ISO/IEC 17025 aplicados ao escopo
/// de calibração volumétrica (balões volumétricos, picnômetros, pipetas, balanças analíticas padrão).
/// </summary>
public class EquipmentIso17025Tests
{
    private Equipment CreateClientVolumetricEquipment()
    {
        return new Equipment(
            labTag: "VOL-CLI-001",
            model: "Classe A 100 mL",
            manufacturer: "Brand",
            serialNumber: "SN-VOL-100",
            instrumentType: "Balão Volumétrico",
            rangeMin: 0m,
            rangeMax: 100m,
            unit: "mL",
            resolution: 0.1m,
            clientId: Guid.NewGuid(),
            name: "Balão Volumétrico 100 mL Classe A",
            softwareVersion: null, // vidraria não possui software nem firmware
            firmwareVersion: null,
            currentLocation: "Laboratório de Calibração de Volume - Triagem",
            acceptanceCriteria: "Capacidade nominal 100 mL, Erro Máximo Permissível (EMP) ± 0,10 mL conforme ISO 1042 / ISO 4787",
            maintenancePlan: "Descontaminação com detergente neutro apropriado para vidraria volumétrica e secagem à temperatura ambiente",
            initialConditionNotes: "Vidraria íntegra, traço de referência do menisco perfeitamente nítido, esmerilhado sem trincas.");
    }

    private StandardEquipment CreateUsableVolumeStandard()
    {
        return new StandardEquipment(
            labTag: "PAD-BAL-001",
            model: "XP205",
            manufacturer: "Mettler Toledo",
            serialNumber: "SN-XP205-VOL-01",
            instrumentType: "Balança Analítica",
            error: 0.0001m,
            expandedUncertainty: 0.0002m,
            coverageFactor: 2.0m,
            lastCalibrationDate: DateTime.UtcNow.AddMonths(-3),
            calibrationDueDate: DateTime.UtcNow.AddMonths(9),
            certificateNumber: "CAL-RBC-MASSA-2026",
            calibratingLaboratory: "Laboratório RBC de Metrologia em Massa",
            resolution: 0.00001m,
            name: "Balança Analítica Semi-Micro XP205",
            softwareVersion: null,
            firmwareVersion: "v1.4.2",
            currentLocation: "Sala Climatizada de Volume - Mesa Antivibratória",
            acceptanceCriteria: "EMA <= 0,0005 g; Incerteza expandida U <= 0,0002 g",
            calibrationIntervalMonths: 12,
            maintenancePlan: "Checagem intermediária mensal com pesos-padrão classe E2 e calibração anual RBC");
    }

    [Fact]
    public void Equipment_ISO17025_Records_ShouldBePreservedAndRetrievable()
    {
        // Arrange
        var equipment = CreateClientVolumetricEquipment();

        // Act - Registro de manutenção/limpeza técnica mandatória para calibração gravimétrica (6.4.13 g)
        var maint = equipment.AddMaintenanceRecord(
            DateTime.UtcNow,
            MaintenanceType.CleaningOrDecontamination,
            "Limpeza química com solução desengraxante e enxágue com água deionizada grau 3 para eliminação de filme graxo nas paredes internas",
            "Técnico de Laboratório",
            "Paredes internas sem formação de gotículas residuais, secagem completa.");

        // Act - Registro de avaria / dano no recebimento (6.4.13 h e ISO 17025 7.4.3)
        var occ = equipment.ReportOccurrence(
            DateTime.UtcNow,
            OccurrenceType.ReceivingNonConformity,
            "Leve desgaste na numeração impressa na base do gargalo, linha de graduação intacta",
            "Fotografado e registrado no formulário de recebimento da ordem de serviço",
            "Recepção de Amostras",
            resolvedDate: DateTime.UtcNow);

        // Assert
        equipment.Name.Should().Be("Balão Volumétrico 100 mL Classe A");
        equipment.CurrentLocation.Should().Be("Laboratório de Calibração de Volume - Triagem");
        equipment.AcceptanceCriteria.Should().Contain("± 0,10 mL");
        equipment.InitialConditionNotes.Should().Contain("Vidraria íntegra");
        equipment.MaintenanceHistory.Should().ContainSingle().Which.Should().Be(maint);
        equipment.OccurrenceHistory.Should().ContainSingle().Which.Should().Be(occ);
    }

    [Fact]
    public void CalibrationRecord_WithUsableStandard_ShouldSucceed()
    {
        // Arrange
        var clientEquipment = CreateClientVolumetricEquipment();
        var standard = CreateUsableVolumeStandard();

        // Act
        var record = new CalibrationRecord(
            clientEquipment,
            standard,
            technicianId: "tech-123",
            technicianName: "Maria Santos",
            calibrationDate: DateTime.UtcNow,
            nextCalibrationDue: DateTime.UtcNow.AddYears(1),
            temperatureCelsius: 20.1m,
            humidityPercent: 55.0m);

        // Assert
        record.StandardEquipmentId.Should().Be(standard.Id);
        record.EquipmentId.Should().Be(clientEquipment.Id);
    }

    [Fact]
    public void CalibrationRecord_WithStandardInCalibration_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var clientEquipment = CreateClientVolumetricEquipment();
        var standard = CreateUsableVolumeStandard();
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
        var clientEquipment = CreateClientVolumetricEquipment();
        var standard = CreateUsableVolumeStandard();
        standard.PutOutOfService("Prato da balança danificado e descalibrado após choque mecânico", "Supervisor da Qualidade");

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
        // Arrange — standard de termômetro de água com calibração vencida
        var clientEquipment = CreateClientVolumetricEquipment();
        var expiredStandard = new StandardEquipment(
            labTag: "PAD-TERM-001",
            model: "Pt100 Digital",
            manufacturer: "Fluke",
            serialNumber: "SN-TERM-99",
            instrumentType: "Termômetro Digital da Água",
            error: 0.01m,
            expandedUncertainty: 0.005m,
            coverageFactor: 2m,
            lastCalibrationDate: DateTime.UtcNow.AddMonths(-25),
            calibrationDueDate: DateTime.UtcNow.AddDays(-1), // expirado ontem
            certificateNumber: "CAL-TERM-EXPIRADO",
            calibratingLaboratory: "Lab Temperatura RBC",
            resolution: 0.01m);

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

    [Fact]
    public void Equipment_VolumetricFlask_WithoutRangeAndResolution_ShouldSucceed()
    {
        // Arrange & Act
        var flask = new Equipment(
            labTag: "BAL-CLI-100",
            model: "Classe A",
            manufacturer: "Vidrolab",
            serialNumber: "SN-BAL-100",
            instrumentType: "Balão Volumétrico",
            rangeMin: null,
            rangeMax: null,
            unit: "mL",
            resolution: null,
            clientId: Guid.NewGuid(),
            name: "Balão Volumétrico de 100 mL",
            nominalCapacity: 100m);

        // Assert
        flask.Should().NotBeNull();
        flask.RangeMin.Should().BeNull();
        flask.RangeMax.Should().BeNull();
        flask.Resolution.Should().BeNull();
        flask.NominalCapacity.Should().Be(100m);
        flask.CalibrationRangeMin.Should().Be(100m);
        flask.CalibrationRangeMax.Should().Be(100m);
        flask.IsWithinCalibrationRange(100m).Should().BeTrue();
        flask.IsWithinCalibrationRange(101m).Should().BeFalse();
    }

    [Fact]
    public void Equipment_UpdateDetails_ShouldUpdatePropertiesAndMarkAsUpdated()
    {
        // Arrange
        var equipment = CreateClientVolumetricEquipment();

        // Act
        equipment.UpdateDetails(
            labTag: "VOL-CLI-001-MOD",
            name: "Balão Volumétrico 100 mL Modificado",
            model: "Classe A Premium",
            manufacturer: "Brand New",
            serialNumber: "SN-VOL-100-NEW",
            instrumentType: "Balão Volumétrico",
            unit: "mL",
            rangeMin: null,
            rangeMax: null,
            resolution: null,
            nominalCapacity: 100m,
            softwareVersion: null,
            firmwareVersion: null,
            currentLocation: "Armário de Vidrarias B",
            acceptanceCriteria: "EMP ±0.08 mL",
            maintenancePlan: "Limpeza com água ultrapura",
            initialConditionNotes: "Perfeito estado");

        // Assert
        equipment.LabTag.Should().Be("VOL-CLI-001-MOD");
        equipment.Name.Should().Be("Balão Volumétrico 100 mL Modificado");
        equipment.Model.Should().Be("Classe A Premium");
        equipment.Manufacturer.Should().Be("Brand New");
        equipment.SerialNumber.Should().Be("SN-VOL-100-NEW");
        equipment.NominalCapacity.Should().Be(100m);
        equipment.Resolution.Should().BeNull();
        equipment.RangeMin.Should().BeNull();
        equipment.CurrentLocation.Should().Be("Armário de Vidrarias B");
        equipment.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void StandardEquipment_UpdateDetails_ShouldUpdatePropertiesAndMarkAsUpdated()
    {
        // Arrange
        var standard = CreateUsableVolumeStandard();

        // Act
        standard.UpdateDetails(
            labTag: "PAD-BAL-001-MOD",
            name: "Balança Mettler XP205 Atualizada",
            model: "XP205 Plus",
            manufacturer: "Mettler Toledo Inc.",
            serialNumber: "SN-XP205-VOL-01-REV",
            instrumentType: "Balança Analítica",
            resolution: 0.00001m,
            rangeMin: 0m,
            rangeMax: 220m,
            calibrationIntervalMonths: 24,
            softwareVersion: "v2.0",
            firmwareVersion: "v1.5",
            currentLocation: "Sala Limpa",
            acceptanceCriteria: "EMA <= 0.0003g",
            maintenancePlan: "Verificação quinzenal",
            referenceMaterialDocumentation: null);

        // Assert
        standard.LabTag.Should().Be("PAD-BAL-001-MOD");
        standard.Name.Should().Be("Balança Mettler XP205 Atualizada");
        standard.Model.Should().Be("XP205 Plus");
        standard.RangeMax.Should().Be(220m);
        standard.CalibrationIntervalMonths.Should().Be(24);
        standard.CurrentLocation.Should().Be("Sala Limpa");
        standard.UpdatedAt.Should().NotBeNull();
    }
}
