using FluentAssertions;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using Xunit;

namespace TracLabs.Domain.Tests;

public class CertificateLifecycleDomainTests
{
    [Fact]
    public void Constructor_ShouldInitializeCertificateInDraftStatus()
    {
        // Arrange
        var calibId = Guid.NewGuid();
        var certNumber = "CERT-2026-00001";

        // Act
        var cert = new Certificate(certNumber, calibId);

        // Assert
        cert.CertificateNumber.Should().Be(certNumber);
        cert.CalibrationRecordId.Should().Be(calibId);
        cert.Status.Should().Be(CertificateStatus.Draft);
        cert.IssuedAt.Should().BeNull();
        cert.IssuedByUserId.Should().BeNull();
        cert.PdfContent.Should().BeNull();
        cert.PdfFileName.Should().BeNull();
    }

    [Fact]
    public void AttachPdf_WhenValid_ShouldStorePdfContentAndFileName()
    {
        // Arrange
        var cert = new Certificate("CERT-2026-00001", Guid.NewGuid());
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4
        var fileName = "CERT-2026-00001.pdf";

        // Act
        cert.AttachPdf(pdfBytes, fileName);

        // Assert
        cert.PdfContent.Should().BeEquivalentTo(pdfBytes);
        cert.PdfFileName.Should().Be(fileName);
        cert.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void AttachPdf_WhenEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var cert = new Certificate("CERT-2026-00001", Guid.NewGuid());

        // Act
        var actNull = () => cert.AttachPdf(null!, "cert.pdf");
        var actEmpty = () => cert.AttachPdf(Array.Empty<byte>(), "cert.pdf");

        // Assert
        actNull.Should().Throw<ArgumentException>();
        actEmpty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Issue_WhenPdfAttachedAndStatusDraft_ShouldTransitionToIssued()
    {
        // Arrange
        var cert = new Certificate("CERT-2026-00001", Guid.NewGuid());
        cert.AttachPdf(new byte[] { 1, 2, 3 }, "CERT-2026-00001.pdf");
        var supervisorId = "supervisor-user-123";

        // Act
        cert.Issue(supervisorId);

        // Assert
        cert.Status.Should().Be(CertificateStatus.Issued);
        cert.IssuedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        cert.IssuedByUserId.Should().Be(supervisorId);
    }

    [Fact]
    public void Issue_WhenPdfNotAttached_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var cert = new Certificate("CERT-2026-00001", Guid.NewGuid());

        // Act
        var act = () => cert.Issue("supervisor-1");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*without an attached PDF*");
    }

    [Fact]
    public void Issue_WhenAlreadyIssued_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var cert = new Certificate("CERT-2026-00001", Guid.NewGuid());
        cert.AttachPdf(new byte[] { 1, 2, 3 }, "CERT-2026-00001.pdf");
        cert.Issue("supervisor-1");

        // Act
        var act = () => cert.Issue("supervisor-2");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot issue a certificate that is in status 'Issued'*");
    }

    [Fact]
    public void Supersede_WhenStatusIssued_ShouldTransitionToSuperseded()
    {
        // Arrange
        var cert = new Certificate("CERT-2026-00001", Guid.NewGuid());
        cert.AttachPdf(new byte[] { 1, 2, 3 }, "CERT-2026-00001.pdf");
        cert.Issue("supervisor-1");

        // Act
        cert.Supersede();

        // Assert
        cert.Status.Should().Be(CertificateStatus.Superseded);
    }

    [Fact]
    public void Supersede_WhenStatusDraft_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var cert = new Certificate("CERT-2026-00001", Guid.NewGuid());

        // Act
        var act = () => cert.Supersede();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only issued certificates can be superseded*");
    }

    [Fact]
    public void Equipment_MarkCertificateIssued_WhenCalibrated_ShouldTransitionToCertificateIssued()
    {
        // Arrange
        var equipment = new Equipment(
            labTag: "VOL-001",
            model: "Balão",
            manufacturer: "Vidros S.A.",
            serialNumber: "SN-001",
            instrumentType: "Balão Volumétrico",
            rangeMin: 0m,
            rangeMax: 100m,
            unit: "mL",
            resolution: 0.1m,
            clientId: Guid.NewGuid(),
            status: EquipmentStatus.Received);

        equipment.StartCalibration();
        
        var standard = new StandardEquipment(
            labTag: "PAD-01",
            model: "M1",
            manufacturer: "M",
            serialNumber: "S1",
            instrumentType: "Balança",
            error: 0m,
            expandedUncertainty: 0.001m,
            coverageFactor: 2.0m,
            lastCalibrationDate: DateTime.UtcNow.AddMonths(-1),
            calibrationDueDate: DateTime.UtcNow.AddMonths(11),
            certificateNumber: "C1",
            calibratingLaboratory: "L1",
            resolution: 0.0001m);

        var calibRecord = new CalibrationRecord(
            equipment,
            standard,
            technicianId: "tech-1",
            technicianName: "Tech One",
            calibrationDate: DateTime.UtcNow,
            nextCalibrationDue: DateTime.UtcNow.AddYears(1),
            temperatureCelsius: 20m,
            humidityPercent: 50m);

        equipment.CompleteCalibration(calibRecord);
        equipment.Status.Should().Be(EquipmentStatus.Calibrated);

        // Act
        equipment.MarkCertificateIssued();

        // Assert
        equipment.Status.Should().Be(EquipmentStatus.CertificateIssued);
    }

    [Fact]
    public void Equipment_MarkCertificateIssued_WhenNotCalibrated_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var equipment = new Equipment(
            labTag: "VOL-002",
            model: "Pipeta",
            manufacturer: "Vidros S.A.",
            serialNumber: "SN-002",
            instrumentType: "Pipeta Graduada",
            rangeMin: 0m,
            rangeMax: 10m,
            unit: "mL",
            resolution: 0.01m,
            clientId: Guid.NewGuid(),
            status: EquipmentStatus.Received);

        // Act
        var act = () => equipment.MarkCertificateIssued();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot issue certificate when equipment is in status 'Received'*");
    }
}
