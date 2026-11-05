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
            new("Balança Analítica", "PAD-BAL-01", "Balança Toledo", "RBC-001/2025", "Laboratório Alfa", 0.0002m, 0.0005m, 2.0m, 0.0001m, 100.0m, false, null, new DateTime(2026, 9, 30)),
            new("Termômetro de Imersão (Água)", "PAD-TERM-01", "Termômetro Incoterm", "RBC-002/2025", "Laboratório Beta", -0.05m, 0.02m, 2.0m, 0.01m, 20.15m, false, null, new DateTime(2026, 11, 15)),
            new("Barômetro", "PAD-BAR-01", "Barômetro Digital", "RBC-003/2025", "Laboratório Gama", 0.1m, 0.2m, 2.0m, 0.1m, 1013.2m, false, null, new DateTime(2027, 7, 20)),
            new("Higrômetro", "PAD-HIG-01", "Termo-Higrômetro Testo", "RBC-004/2025", "Laboratório Delta", -1.0m, 2.0m, 2.0m, 0.1m, 55.0m, false, null, new DateTime(2026, 8, 10))
        };

        var points = new List<CertificateMeasurementPointDto>
        {
            new(1, 2.5m, 2.498m, -0.002m, "mL"),
            new(2, 2.5m, 2.498m, -0.002m, "mL"),
            new(3, 12.5m, 12.495m, -0.005m, "mL"),
            new(4, 12.5m, 12.495m, -0.005m, "mL"),
            new(5, 25.0m, 24.986m, -0.014m, "mL"),
            new(6, 25.0m, 24.987m, -0.013m, "mL")
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
            WaterDensityGcm3: 0.998203m,
            AirDensityGcm3: 0.001198m,
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
            MeasurementPoints: points,
            ResultPoints: new List<CertificateResultPointDto>
            {
                new(2.5m, 2.4981m, -0.0019m, 0.0042m, 2.0m, 35m),
                new(12.5m, 12.4950m, -0.0050m, 0.0065m, 2.0m, 48m),
                new(25.0m, 24.9863m, -0.0137m, 0.0070m, 2.0m, 120m)
            }
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

    [Fact]
    public async Task GenerateVolumeCertificatePdfAsync_ShouldGenerateArtifactPdfAndPageImage()
    {
        // Arrange
        var reportData = CreateSampleReportData();
        var options = new CertificateCustomizationOptions(
            LaboratoryName: "TracLabs Metrologia & Ensaios",
            LaboratoryTaxId: "CNPJ 12.345.678/0001-90",
            LaboratoryAddress: "Av. Visconde do Rio Branco, 300 - Centro, Niterói - RJ",
            LaboratoryContact: "contato@traclabs.com.br | (21) 3600-0000",
            AccreditationNumber: "Laboratório de Calibração Acreditado pela Cgcre/INMETRO sob nº CRL 0999",
            PrimaryColorHex: "#1E3A8A", // Azul Marinho Metrológico
            TemplateStyle: CertificateTemplateStyle.ClassicRbc,
            ShowUncertaintyBudget: true,
            ShowDetailedPoints: true,
            ShowSignatures: true,
            CustomNotes: "Calibração realizada conforme Procedimento Interno POP-MET-012 rev. 04. " +
                         "O instrumento foi calibrado utilizando água destilada com condutividade inferior a 5 uS/cm."
        );

        // Act
        var pdfBytes = await _generator.GenerateVolumeCertificatePdfAsync(reportData, options);
        var images = await _generator.GenerateVolumeCertificateImagesAsync(reportData, options);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(1000);
        images.Should().NotBeEmpty();

        // Salva os arquivos no diretório de artefatos para disponibilização imediata ao usuário
        var artifactDir = @"C:\Users\jpbar\.gemini\antigravity\brain\8176b307-a48d-4c8a-920d-dd04ab9259c5";
        if (Directory.Exists(artifactDir))
        {
            var pdfPath = Path.Combine(artifactDir, "certificado_calibracao_CERT-2026-00042.pdf");
            File.WriteAllBytes(pdfPath, pdfBytes);

            for (int i = 0; i < images.Count; i++)
            {
                var imgPath = Path.Combine(artifactDir, $"certificado_calibracao_CERT-2026-00042_pagina{i + 1}.png");
                File.WriteAllBytes(imgPath, images[i]);
            }
        }
    }
}
