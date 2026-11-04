using TracLabs.Application.Features.Certificates.DTOs;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Certificates.Common;

/// <summary>
/// Construtor de DTO de relatório para certificados de calibração.
/// Extrai e formata dados metrológicos, rastreabilidade RBC e dados cadastrais da calibração.
/// </summary>
public static class CertificateReportBuilder
{
    public static CertificateReportDto Build(Certificate cert, string? supervisorName = null)
    {
        ArgumentNullException.ThrowIfNull(cert);

        var record = cert.CalibrationRecord
            ?? throw new InvalidOperationException($"O registro de calibração associado ao certificado '{cert.CertificateNumber}' não foi carregado.");

        var equipment = record.Equipment
            ?? throw new InvalidOperationException($"O equipamento associado à calibração '{record.Id}' não foi carregado.");

        var client = equipment.Client
            ?? throw new InvalidOperationException($"O cliente associado ao equipamento '{equipment.LabTag}' não foi carregado.");

        // Identifica condições ambientais específicas a partir dos padrões utilizados
        var liquidThermometer = record.StandardsUsed.FirstOrDefault(s => s.Role == StandardRole.LiquidThermometer);
        var barometer = record.StandardsUsed.FirstOrDefault(s => s.Role == StandardRole.Barometer);

        var standardsUsedDtos = record.StandardsUsed.Select(s => new CertificateStandardUsedItemDto(
            Role: TranslateStandardRole(s.Role),
            LabTag: s.LabTag,
            Description: s.StandardEquipment?.Name ?? TranslateStandardRole(s.Role),
            CertificateNumber: s.CertificateNumber,
            CalibratingLaboratory: s.CalibratingLaboratory ?? "Laboratório RBC Acreditado Cgcre",
            Correction: s.Correction,
            ExpandedUncertainty: s.ExpandedUncertainty,
            CoverageFactor: s.CoverageFactor,
            Resolution: s.Resolution,
            OperatingValue: s.OperatingValue,
            IsExtrapolated: s.IsExtrapolated,
            WarningMessage: s.WarningMessage
        )).ToList();

        var measurementPointsDtos = record.MeasurementPoints.Select((p, idx) => new CertificateMeasurementPointDto(
            RunNumber: idx + 1,
            NominalVolumeMl: p.NominalValue,
            IndicatedVolumeMl: p.IndicatedValue,
            ErrorMl: p.Error,
            Unit: p.Unit
        )).ToList();

        // Nominal volume do equipamento ou do primeiro ponto
        var nominalVolume = measurementPointsDtos.FirstOrDefault()?.NominalVolumeMl ?? equipment.RangeMax;
        var calculatedVolumeV20 = nominalVolume + record.MeanError;

        return new CertificateReportDto(
            CertificateId: cert.Id,
            CertificateNumber: cert.CertificateNumber,
            Status: cert.Status,
            IssuedAt: cert.IssuedAt,
            IssuedByUserId: cert.IssuedByUserId,
            SupervisorName: supervisorName ?? "Responsável Técnico",
            CalibrationDate: record.CalibrationDate,
            NextCalibrationDue: record.NextCalibrationDue,
            TechnicianId: record.TechnicianId,
            TechnicianName: record.TechnicianName,
            // Cliente
            ClientId: client.Id,
            ClientName: client.Name,
            ClientTaxId: client.TaxId,
            ClientStreet: client.Street,
            ClientCity: client.City,
            ClientState: client.State,
            ClientPostalCode: client.PostalCode,
            ClientContactPerson: client.ContactPerson,
            // Equipamento
            EquipmentId: equipment.Id,
            EquipmentLabTag: equipment.LabTag,
            EquipmentName: equipment.Name,
            EquipmentInstrumentType: equipment.InstrumentType,
            EquipmentManufacturer: equipment.Manufacturer,
            EquipmentModel: equipment.Model,
            EquipmentSerialNumber: equipment.SerialNumber,
            EquipmentRangeMin: equipment.RangeMin,
            EquipmentRangeMax: equipment.RangeMax,
            EquipmentCalibrationRangeMin: equipment.CalibrationRangeMin,
            EquipmentCalibrationRangeMax: equipment.CalibrationRangeMax,
            EquipmentUnit: equipment.Unit,
            EquipmentResolution: equipment.Resolution,
            // Condições Ambientais
            AmbientTemperatureCelsius: record.TemperatureCelsius,
            RelativeHumidityPercent: record.HumidityPercent,
            WaterTemperatureCelsius: liquidThermometer?.OperatingValue,
            AtmosphericPressureHpa: barometer?.OperatingValue,
            // Resultados
            NominalVolumeMl: nominalVolume,
            CalculatedVolumeV20Ml: calculatedVolumeV20,
            SystematicErrorMl: record.MeanError,
            MaxAbsoluteErrorMl: record.MaxAbsoluteError,
            CombinedUncertaintyMl: record.CombinedStandardUncertainty,
            ExpandedUncertaintyMl: record.ExpandedUncertainty,
            CoverageFactor: record.CoverageFactor,
            EffectiveDegreesOfFreedom: null,
            Result: record.Result,
            Observations: record.Observations,
            // Coleções
            StandardsUsed: standardsUsedDtos,
            MeasurementPoints: measurementPointsDtos
        );
    }

    private static string TranslateStandardRole(StandardRole role) => role switch
    {
        StandardRole.Balance => "Balança Analítica",
        StandardRole.LiquidThermometer => "Termômetro de Imersão (Água)",
        StandardRole.AmbientThermometer => "Termômetro Ambiente",
        StandardRole.Barometer => "Barômetro",
        StandardRole.Hygrometer => "Higrômetro",
        StandardRole.ReferenceWeights => "Pesos-Padrão",
        _ => role.ToString()
    };
}
