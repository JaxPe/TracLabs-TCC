using FluentAssertions;
using System.Text;
using TracLabs.Application.Features.Certificates.DTOs;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using TracLabs.Infrastructure.Services.Pdf;
using Xunit;

namespace TracLabs.Application.Tests;

public class QuestPdfCertificateGeneratorTests
{
    private readonly QuestPdfCertificateGenerator _generator = new();

    private static CertificateReportDto CreateSampleReportData()
    {
        var standards = new List<CertificateStandardUsedItemDto>
        {
            new("Balança Analítica", "PAD-BAL-01", "Balança Toledo", "RBC-001/2025", "Laboratório Alfa", 0.0002m, 0.0005m, 2.0m, 0.0001m, 100.0m, false, null),
            new("Termômetro de Imersão (Água)", "PAD-TERM-01", "Termômetro Incoterm", "RBC-002/2025", "Laboratório Beta", -0.05m, 0.02m, 2.0m, 0.01m, 20.15m, false, null),
            new("Barômetro", "PAD-BAR-01", "Barômetro Digital", "RBC-003/2025", "Laboratório Gama", 0.1m, 0.2m, 2.0m, 0.1m, 1013.2m, false, null),
            new("Higrômetro", "PAD-HIG-01", "Termo-Higrômetro Testo", "RBC-004/2025", "Laboratório Delta", -1.0m, 2.0m, 2.0m, 0.1m, 55.0m, false, null)
        };

        var points = new List<CertificateMeasurementPointDto>
        {
            new(1, 100.0m, 99.985m, -0.015m, "mL"),
            new(2, 100.0m, 99.988m, -0.012m, "mL"),
            new(3, 100.0m, 99.986m, -0.014m, "mL")
        };

        return new CertificateReportDto(
            CertificateId: Guid.NewGuid(),
            CertificateNumber: "CERT-2026-00042",
            Status: CertificateStatus.Issued,
            IssuedAt: DateTime.UtcNow,
            IssuedByUserId: "supervisor-123",
            SupervisorName: "Dr. João Metrologista",
            CalibrationDate: DateTime.UtcNow.AddDays(-1),
            NextCalibrationDue: DateTime.UtcNow.AddYears(1),
            TechnicianId: "tech-456",
            TechnicianName: "Maria Técnica",
            ClientId: Guid.NewGuid(),
            ClientName: "Indústria Química Brasileira S.A.",
            ClientTaxId: "12345678000190",
            ClientStreet: "Av. Industrial, 500",
            ClientCity: "Niterói",
            ClientState: "RJ",
            ClientPostalCode: "24000-000",
            ClientContactPerson: "Carlos Gerente",
            EquipmentId: Guid.NewGuid(),
            EquipmentLabTag: "BAL-CLI-001",
            EquipmentName: "Balão Volumétrico 100 mL",
            EquipmentInstrumentType: "Balão Volumétrico",
            EquipmentManufacturer: "Vidraria Nacional",
            EquipmentModel: "Classe A Rolha Poli",
            EquipmentSerialNumber: "SN-987654",
            EquipmentRangeMin: 0m,
            EquipmentRangeMax: 100m,
            EquipmentCalibrationRangeMin: 0m,
            EquipmentCalibrationRangeMax: 100m,
            EquipmentUnit: "mL",
            EquipmentResolution: 0.1m,
            AmbientTemperatureCelsius: 20.5m,
            RelativeHumidityPercent: 55.0m,
            WaterTemperatureCelsius: 20.15m,
            AtmosphericPressureHpa: 1013.2m,
            NominalVolumeMl: 100.0m,
            CalculatedVolumeV20Ml: 99.9863m,
            SystematicErrorMl: -0.0137m,
            MaxAbsoluteErrorMl: 0.0150m,
            CombinedUncertaintyMl: 0.0035m,
            ExpandedUncertaintyMl: 0.0070m,
            CoverageFactor: 2.0m,
            EffectiveDegreesOfFreedom: null,
            Result: CalibrationResult.Conforming,
            Observations: "Equipamento em perfeito estado de conservação.",
            StandardsUsed: standards,
            MeasurementPoints: points
        );
    }

    [Fact]
    public async Task GenerateVolumeCertificatePdfAsync_WithDefaultOptions_ShouldReturnValidPdfBytes()
    {
        // Arrange
        var reportData = CreateSampleReportData();
        var options = new CertificateCustomizationOptions();

        // Act
        var pdfBytes = await _generator.GenerateVolumeCertificatePdfAsync(reportData, options);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(1000);

        // Verifica o cabeçalho oficial de arquivo PDF (%PDF-1.)
        var header = Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray());
        header.Should().Be("%PDF-");
    }

    [Fact]
    public async Task GenerateVolumeCertificatePdfAsync_WithCustomColorsAndHiddenPoints_ShouldReturnValidPdf()
    {
        // Arrange
        var reportData = CreateSampleReportData();
        var options = new CertificateCustomizationOptions(
            LaboratoryName: "Laboratório CalibraMais",
            LaboratoryTaxId: "CNPJ 99.888.777/0001-66",
            PrimaryColorHex: "#0D9488", // Verde Petróleo
            TemplateStyle: CertificateTemplateStyle.ModernClean,
            ShowDetailedPoints: false,
            ShowSignatures: true,
            CustomNotes: "Calibração realizada nas instalações do cliente."
        );

        // Act
        var pdfBytes = await _generator.GenerateVolumeCertificatePdfAsync(reportData, options);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(1000);

        var header = Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray());
        header.Should().Be("%PDF-");
    }
}
