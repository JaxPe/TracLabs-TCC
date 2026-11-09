using FluentAssertions;
using NSubstitute;
using TracLabs.Application.Features.Certificates.Commands.GenerateCertificatePdf;
using TracLabs.Application.Features.Certificates.Commands.IssueCertificate;
using TracLabs.Application.Features.Certificates.Commands.SupersedeCertificate;
using TracLabs.Application.Features.Certificates.DTOs;
using TracLabs.Application.Features.Certificates.Queries.DownloadCertificatePdf;
using TracLabs.Application.Features.Certificates.Queries.GetCertificateById;
using TracLabs.Application.Features.Certificates.Queries.GetCertificateByNumber;
using TracLabs.Application.Features.Certificates.Queries.PreviewCertificatePdf;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Application.Interfaces.Services;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;
using Xunit;

namespace TracLabs.Application.Tests;

public class CertificateUseCasesTests
{
    private readonly ICertificateRepository _certificateRepo = Substitute.For<ICertificateRepository>();
    private readonly IEquipmentRepository _equipmentRepo = Substitute.For<IEquipmentRepository>();
    private readonly ICertificatePdfGenerator _pdfGenerator = Substitute.For<ICertificatePdfGenerator>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private Certificate CreateTestCertificateWithGraph(bool attachPdf = false, CertificateStatus status = CertificateStatus.Draft)
    {
        var client = new Client(
            name: "Cliente Laboratorial Ltda",
            taxId: "12345678000195",
            taxIdType: TaxIdType.Cnpj,
            email: "contato@cliente.com",
            phone: "2199999999",
            contactPerson: "Carlos Silva",
            street: "Rua das Flores, 123",
            city: "Niterói",
            state: "RJ",
            postalCode: "24000-000");

        var equipment = new Equipment(
            labTag: "TAG-CLI-01",
            model: "Mod-100",
            manufacturer: "Vidros Lab",
            serialNumber: "SN-100",
            instrumentType: "Balão Volumétrico",
            rangeMin: 0m,
            rangeMax: 100m,
            unit: "mL",
            resolution: 0.1m,
            clientId: client.Id,
            status: EquipmentStatus.Calibrated);

        typeof(Equipment).GetProperty(nameof(Equipment.Client))!.SetValue(equipment, client);

        var standard = new StandardEquipment(
            labTag: "PAD-BAL",
            model: "M1",
            manufacturer: "M",
            serialNumber: "S1",
            instrumentType: "Balança",
            error: 0.0001m,
            expandedUncertainty: 0.0005m,
            coverageFactor: 2.0m,
            lastCalibrationDate: DateTime.UtcNow.AddMonths(-1),
            calibrationDueDate: DateTime.UtcNow.AddMonths(11),
            certificateNumber: "CERT-PAD-01",
            calibratingLaboratory: "RBC Lab",
            resolution: 0.0001m);

        var record = new CalibrationRecord(
            equipment,
            standard,
            technicianId: "tech-01",
            technicianName: "João Silva",
            calibrationDate: DateTime.UtcNow.AddDays(-1),
            nextCalibrationDue: DateTime.UtcNow.AddYears(1),
            temperatureCelsius: 20.0m,
            humidityPercent: 55.0m);

        record.RecordResults(
            measurementPoints: new List<MeasurementPoint> { new(100m, 99.98m, "mL") },
            meanError: -0.02m,
            maxAbsoluteError: 0.02m,
            combinedStandardUncertainty: 0.005m,
            expandedUncertainty: 0.010m,
            coverageFactor: 2.0m,
            result: CalibrationResult.Conforming,
            observations: "Tudo nos conformes");

        typeof(CalibrationRecord).GetProperty(nameof(CalibrationRecord.Equipment))!.SetValue(record, equipment);

        var cert = new Certificate("CERT-2026-00001", record.Id);
        typeof(Certificate).GetProperty(nameof(Certificate.CalibrationRecord))!.SetValue(cert, record);

        if (attachPdf)
        {
            cert.AttachPdf(new byte[] { 0x25, 0x50, 0x44, 0x46 }, "CERT-2026-00001.pdf");
        }

        if (status == CertificateStatus.Issued)
        {
            if (!attachPdf)
                cert.AttachPdf(new byte[] { 0x25, 0x50, 0x44, 0x46 }, "CERT-2026-00001.pdf");
            cert.Issue("supervisor-01");
        }
        else if (status == CertificateStatus.Superseded)
        {
            if (!attachPdf)
                cert.AttachPdf(new byte[] { 0x25, 0x50, 0x44, 0x46 }, "CERT-2026-00001.pdf");
            cert.Issue("supervisor-01");
            cert.Supersede();
        }

        return cert;
    }

    [Fact]
    public async Task GenerateCertificatePdf_ShouldRenderPdfAndAttachToCertificate()
    {
        // Arrange
        var cert = CreateTestCertificateWithGraph(attachPdf: false);
        _certificateRepo.GetWithDetailsAsync(cert.Id, Arg.Any<CancellationToken>()).Returns(cert);
        _pdfGenerator.GenerateVolumeCertificatePdfAsync(Arg.Any<CertificateReportDto>(), Arg.Any<CertificateCustomizationOptions>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1, 2, 3, 4, 5 });

        var handler = new GenerateCertificatePdfCommandHandler(_certificateRepo, _pdfGenerator, _unitOfWork);
        var command = new GenerateCertificatePdfCommand(cert.Id, new CertificateCustomizationOptions(LaboratoryName: "Meu Lab"));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.HasPdf.Should().BeTrue();
        result.PdfFileName.Should().Be("CERT-2026-00001.pdf");
        cert.PdfContent.Should().NotBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PreviewCertificatePdf_ShouldReturnBytesWithoutModifyingDatabase()
    {
        // Arrange
        var cert = CreateTestCertificateWithGraph(attachPdf: false);
        _certificateRepo.GetWithDetailsAsync(cert.Id, Arg.Any<CancellationToken>()).Returns(cert);
        var expectedBytes = new byte[] { 9, 8, 7 };
        _pdfGenerator.GenerateVolumeCertificatePdfAsync(Arg.Any<CertificateReportDto>(), Arg.Any<CertificateCustomizationOptions>(), Arg.Any<CancellationToken>())
            .Returns(expectedBytes);

        var handler = new PreviewCertificatePdfQueryHandler(_certificateRepo, _pdfGenerator);
        var query = new PreviewCertificatePdfQuery(cert.Id, new CertificateCustomizationOptions());

        // Act
        var (content, fileName) = await handler.Handle(query, CancellationToken.None);

        // Assert
        content.Should().BeEquivalentTo(expectedBytes);
        fileName.Should().Contain("PREVIEW");
        cert.PdfContent.Should().BeNull(); // Banco não foi alterado
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IssueCertificate_WhenPdfAttached_ShouldTransitionCertificateAndEquipmentStatus()
    {
        // Arrange
        var cert = CreateTestCertificateWithGraph(attachPdf: true);
        _certificateRepo.GetWithDetailsAsync(cert.Id, Arg.Any<CancellationToken>()).Returns(cert);
        _currentUserService.UserId.Returns("supervisor-42");
        _pdfGenerator.GenerateVolumeCertificatePdfAsync(
            Arg.Any<CertificateReportDto>(),
            Arg.Any<CertificateCustomizationOptions>(),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(new byte[] { 1, 2, 3 }));

        var handler = new IssueCertificateCommandHandler(_certificateRepo, _equipmentRepo, _pdfGenerator, _currentUserService, _unitOfWork);
        var command = new IssueCertificateCommand(cert.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CertificateStatus.Issued);
        cert.Status.Should().Be(CertificateStatus.Issued);
        cert.IssuedByUserId.Should().Be("supervisor-42");
        cert.CalibrationRecord!.Equipment!.Status.Should().Be(EquipmentStatus.CertificateIssued);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IssueCertificate_WhenPdfNotAttached_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var cert = CreateTestCertificateWithGraph(attachPdf: false);
        _certificateRepo.GetWithDetailsAsync(cert.Id, Arg.Any<CancellationToken>()).Returns(cert);
        _currentUserService.UserId.Returns("supervisor-42");

        var handler = new IssueCertificateCommandHandler(_certificateRepo, _equipmentRepo, _pdfGenerator, _currentUserService, _unitOfWork);
        var command = new IssueCertificateCommand(cert.Id);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*without an attached PDF*");
    }

    [Fact]
    public async Task SupersedeCertificate_WhenIssued_ShouldTransitionToSuperseded()
    {
        // Arrange
        var cert = CreateTestCertificateWithGraph(attachPdf: true, status: CertificateStatus.Issued);
        _certificateRepo.GetWithDetailsAsync(cert.Id, Arg.Any<CancellationToken>()).Returns(cert);

        _pdfGenerator.GenerateVolumeCertificatePdfAsync(
            Arg.Any<CertificateReportDto>(),
            Arg.Any<CertificateCustomizationOptions>(),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(new byte[] { 99, 88, 77 }));

        var handler = new SupersedeCertificateCommandHandler(_certificateRepo, _pdfGenerator, _unitOfWork);
        var command = new SupersedeCertificateCommand(cert.Id, "Erro de digitação no número de série");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CertificateStatus.Superseded);
        cert.Status.Should().Be(CertificateStatus.Superseded);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DownloadCertificatePdf_WhenPdfAttached_ShouldReturnBytes()
    {
        // Arrange
        var cert = CreateTestCertificateWithGraph(attachPdf: true);
        _certificateRepo.GetByIdAsync(cert.Id, Arg.Any<CancellationToken>()).Returns(cert);

        var handler = new DownloadCertificatePdfQueryHandler(_certificateRepo);
        var query = new DownloadCertificatePdfQuery(cert.Id);

        // Act
        var (content, fileName) = await handler.Handle(query, CancellationToken.None);

        // Assert
        content.Should().NotBeNull();
        content.Length.Should().Be(4);
        fileName.Should().Be("CERT-2026-00001.pdf");
    }

    [Fact]
    public async Task DownloadCertificatePdf_WhenPdfNotAttached_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var cert = CreateTestCertificateWithGraph(attachPdf: false);
        _certificateRepo.GetByIdAsync(cert.Id, Arg.Any<CancellationToken>()).Returns(cert);

        var handler = new DownloadCertificatePdfQueryHandler(_certificateRepo);
        var query = new DownloadCertificatePdfQuery(cert.Id);

        // Act
        var act = () => handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ainda não foi gerado*");
    }

    [Fact]
    public async Task GetCertificateById_ShouldReturnDetailedDto()
    {
        // Arrange
        var cert = CreateTestCertificateWithGraph(attachPdf: true);
        _certificateRepo.GetWithDetailsAsync(cert.Id, Arg.Any<CancellationToken>()).Returns(cert);

        var handler = new GetCertificateByIdQueryHandler(_certificateRepo);
        var query = new GetCertificateByIdQuery(cert.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CertificateNumber.Should().Be("CERT-2026-00001");
        result.ClientName.Should().Be("Cliente Laboratorial Ltda");
        result.EquipmentTag.Should().Be("TAG-CLI-01");
        result.HasPdf.Should().BeTrue();
    }

    [Fact]
    public async Task GetCertificateByNumber_ShouldReturnDetailedDto()
    {
        // Arrange
        var cert = CreateTestCertificateWithGraph(attachPdf: true);
        _certificateRepo.GetByCertificateNumberAsync("CERT-2026-00001", Arg.Any<CancellationToken>()).Returns(cert);
        _certificateRepo.GetWithDetailsAsync(cert.Id, Arg.Any<CancellationToken>()).Returns(cert);

        var handler = new GetCertificateByNumberQueryHandler(_certificateRepo);
        var query = new GetCertificateByNumberQuery("CERT-2026-00001");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CertificateNumber.Should().Be("CERT-2026-00001");
        result.ClientName.Should().Be("Cliente Laboratorial Ltda");
    }
}
