using MediatR;
using TracLabs.Application.Features.Certificates.Common;
using TracLabs.Application.Features.Certificates.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Application.Interfaces.Services;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Certificates.Commands.SupersedeCertificate;

public record SupersedeCertificateCommand(
    Guid CertificateId,
    string Reason
) : IRequest<CertificateDetailDto>;

public class SupersedeCertificateCommandHandler : IRequestHandler<SupersedeCertificateCommand, CertificateDetailDto>
{
    private readonly ICertificateRepository _certificateRepository;
    private readonly ICertificatePdfGenerator _pdfGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public SupersedeCertificateCommandHandler(
        ICertificateRepository certificateRepository,
        ICertificatePdfGenerator pdfGenerator,
        IUnitOfWork unitOfWork)
    {
        _certificateRepository = certificateRepository;
        _pdfGenerator = pdfGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<CertificateDetailDto> Handle(SupersedeCertificateCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("A justificativa de cancelamento/substituição é obrigatória.", nameof(request.Reason));

        var cert = await _certificateRepository.GetWithDetailsAsync(request.CertificateId, cancellationToken);
        if (cert == null)
            throw new KeyNotFoundException($"Certificado com ID '{request.CertificateId}' não encontrado.");

        cert.Supersede();

        // Regenera o laudo com o status 'Superseded' e o carimbo oficial [SUBSTITUÍDO / CANCELADO] (ISO 17025 7.8.8.2)
        var reportDto = CertificateReportBuilder.Build(cert);
        var options = new CertificateCustomizationOptions(ShowDetailedPoints: false);
        var pdfBytes = await _pdfGenerator.GenerateVolumeCertificatePdfAsync(reportDto, options, cancellationToken);
        cert.AttachPdf(pdfBytes, $"{cert.CertificateNumber}_CANCELADO.pdf");

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var record = cert.CalibrationRecord;
        var equipment = record?.Equipment;
        var client = equipment?.Client;
        var firstPoint = record?.MeasurementPoints.FirstOrDefault();
        var nominalVolume = firstPoint?.NominalValue ?? equipment?.NominalCapacity ?? equipment?.RangeMax ?? 0m;

        return new CertificateDetailDto(
            Id: cert.Id,
            CertificateNumber: cert.CertificateNumber,
            CalibrationRecordId: record?.Id ?? Guid.Empty,
            EquipmentId: equipment?.Id ?? Guid.Empty,
            EquipmentTag: equipment?.LabTag ?? string.Empty,
            EquipmentName: equipment?.Name ?? string.Empty,
            ClientId: client?.Id ?? Guid.Empty,
            ClientName: client?.Name ?? string.Empty,
            Status: cert.Status,
            CalibrationDate: record?.CalibrationDate ?? DateTime.UtcNow,
            NextCalibrationDue: record?.NextCalibrationDue ?? DateTime.UtcNow,
            IssuedAt: cert.IssuedAt,
            IssuedByUserId: cert.IssuedByUserId,
            PdfFileName: cert.PdfFileName,
            HasPdf: cert.PdfContent != null && cert.PdfContent.Length > 0,
            NominalVolumeMl: nominalVolume,
            SystematicErrorMl: record?.MeanError ?? 0m,
            ExpandedUncertaintyMl: record?.ExpandedUncertainty ?? 0m,
            CoverageFactor: record?.CoverageFactor ?? 0m,
            Result: record?.Result ?? CalibrationResult.Pending,
            Observations: record?.Observations
        );
    }
}
