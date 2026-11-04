using MediatR;
using TracLabs.Application.Features.Certificates.Common;
using TracLabs.Application.Features.Certificates.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Application.Interfaces.Services;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Certificates.Commands.GenerateCertificatePdf;

public record GenerateCertificatePdfCommand(
    Guid CertificateId,
    CertificateCustomizationOptions? Customization = null
) : IRequest<CertificateDetailDto>;

public class GenerateCertificatePdfCommandHandler : IRequestHandler<GenerateCertificatePdfCommand, CertificateDetailDto>
{
    private readonly ICertificateRepository _certificateRepository;
    private readonly ICertificatePdfGenerator _pdfGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateCertificatePdfCommandHandler(
        ICertificateRepository certificateRepository,
        ICertificatePdfGenerator pdfGenerator,
        IUnitOfWork unitOfWork)
    {
        _certificateRepository = certificateRepository;
        _pdfGenerator = pdfGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<CertificateDetailDto> Handle(GenerateCertificatePdfCommand request, CancellationToken cancellationToken)
    {
        var cert = await _certificateRepository.GetWithDetailsAsync(request.CertificateId, cancellationToken);
        if (cert == null)
            throw new KeyNotFoundException($"Certificado com ID '{request.CertificateId}' não encontrado.");

        if (cert.Status == CertificateStatus.Superseded)
            throw new InvalidOperationException($"Não é possível gerar novo PDF para um certificado que já foi substituído/cancelado ('{cert.CertificateNumber}').");

        var options = request.Customization ?? new CertificateCustomizationOptions();
        var reportDto = CertificateReportBuilder.Build(cert);

        var pdfBytes = await _pdfGenerator.GenerateVolumeCertificatePdfAsync(reportDto, options, cancellationToken);
        var fileName = $"{cert.CertificateNumber}.pdf";

        cert.AttachPdf(pdfBytes, fileName);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var record = cert.CalibrationRecord!;
        var equipment = record.Equipment!;
        var client = equipment.Client!;

        return new CertificateDetailDto(
            Id: cert.Id,
            CertificateNumber: cert.CertificateNumber,
            CalibrationRecordId: record.Id,
            EquipmentId: equipment.Id,
            EquipmentTag: equipment.LabTag,
            EquipmentName: equipment.Name,
            ClientId: client.Id,
            ClientName: client.Name,
            Status: cert.Status,
            CalibrationDate: record.CalibrationDate,
            NextCalibrationDue: record.NextCalibrationDue,
            IssuedAt: cert.IssuedAt,
            IssuedByUserId: cert.IssuedByUserId,
            PdfFileName: cert.PdfFileName,
            HasPdf: cert.PdfContent != null && cert.PdfContent.Length > 0,
            NominalVolumeMl: reportDto.NominalVolumeMl,
            SystematicErrorMl: record.MeanError,
            ExpandedUncertaintyMl: record.ExpandedUncertainty,
            CoverageFactor: record.CoverageFactor,
            Result: record.Result,
            Observations: record.Observations
        );
    }
}
