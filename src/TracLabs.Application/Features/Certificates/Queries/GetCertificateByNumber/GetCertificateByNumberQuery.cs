using MediatR;
using TracLabs.Application.Features.Certificates.DTOs;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Certificates.Queries.GetCertificateByNumber;

public record GetCertificateByNumberQuery(string CertificateNumber) : IRequest<CertificateDetailDto>;

public class GetCertificateByNumberQueryHandler : IRequestHandler<GetCertificateByNumberQuery, CertificateDetailDto>
{
    private readonly ICertificateRepository _certificateRepository;

    public GetCertificateByNumberQueryHandler(ICertificateRepository certificateRepository)
    {
        _certificateRepository = certificateRepository;
    }

    public async Task<CertificateDetailDto> Handle(GetCertificateByNumberQuery request, CancellationToken cancellationToken)
    {
        var cert = await _certificateRepository.GetByCertificateNumberAsync(request.CertificateNumber, cancellationToken);
        if (cert == null)
            throw new KeyNotFoundException($"Certificado com número '{request.CertificateNumber}' não encontrado.");

        var fullCert = await _certificateRepository.GetWithDetailsAsync(cert.Id, cancellationToken) ?? cert;

        var record = fullCert.CalibrationRecord;
        var equipment = record?.Equipment;
        var client = equipment?.Client;
        var firstPoint = record?.MeasurementPoints.FirstOrDefault();
        var nominalVolume = firstPoint?.NominalValue ?? equipment?.RangeMax ?? 0m;

        return new CertificateDetailDto(
            Id: fullCert.Id,
            CertificateNumber: fullCert.CertificateNumber,
            CalibrationRecordId: record?.Id ?? Guid.Empty,
            EquipmentId: equipment?.Id ?? Guid.Empty,
            EquipmentTag: equipment?.LabTag ?? string.Empty,
            EquipmentName: equipment?.Name ?? string.Empty,
            ClientId: client?.Id ?? Guid.Empty,
            ClientName: client?.Name ?? string.Empty,
            Status: fullCert.Status,
            CalibrationDate: record?.CalibrationDate ?? DateTime.UtcNow,
            NextCalibrationDue: record?.NextCalibrationDue ?? DateTime.UtcNow,
            IssuedAt: fullCert.IssuedAt,
            IssuedByUserId: fullCert.IssuedByUserId,
            PdfFileName: fullCert.PdfFileName,
            HasPdf: fullCert.PdfContent != null && fullCert.PdfContent.Length > 0,
            NominalVolumeMl: nominalVolume,
            SystematicErrorMl: record?.MeanError ?? 0m,
            ExpandedUncertaintyMl: record?.ExpandedUncertainty ?? 0m,
            CoverageFactor: record?.CoverageFactor ?? 0m,
            Result: record?.Result ?? CalibrationResult.Pending,
            Observations: record?.Observations
        );
    }
}
