using MediatR;
using TracLabs.Application.Features.Certificates.DTOs;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Certificates.Queries.GetCertificateById;

public record GetCertificateByIdQuery(Guid Id) : IRequest<CertificateDetailDto>;

public class GetCertificateByIdQueryHandler : IRequestHandler<GetCertificateByIdQuery, CertificateDetailDto>
{
    private readonly ICertificateRepository _certificateRepository;

    public GetCertificateByIdQueryHandler(ICertificateRepository certificateRepository)
    {
        _certificateRepository = certificateRepository;
    }

    public async Task<CertificateDetailDto> Handle(GetCertificateByIdQuery request, CancellationToken cancellationToken)
    {
        var cert = await _certificateRepository.GetWithDetailsAsync(request.Id, cancellationToken);
        if (cert == null)
            throw new KeyNotFoundException($"Certificado com ID '{request.Id}' não encontrado.");

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
