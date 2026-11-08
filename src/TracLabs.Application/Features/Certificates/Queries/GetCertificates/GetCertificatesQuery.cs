using MediatR;
using TracLabs.Application.Features.Certificates.DTOs;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Certificates.Queries.GetCertificates;

public record GetCertificatesQuery(CertificateStatus? Status = null) : IRequest<IReadOnlyList<CertificateSummaryDto>>;

public class GetCertificatesQueryHandler : IRequestHandler<GetCertificatesQuery, IReadOnlyList<CertificateSummaryDto>>
{
    private readonly ICertificateRepository _certificateRepository;

    public GetCertificatesQueryHandler(ICertificateRepository certificateRepository)
    {
        _certificateRepository = certificateRepository;
    }

    public async Task<IReadOnlyList<CertificateSummaryDto>> Handle(GetCertificatesQuery request, CancellationToken cancellationToken)
    {
        var certificates = await _certificateRepository.GetAllAsync(request.Status, cancellationToken);

        return certificates.Select(c => new CertificateSummaryDto(
            Id: c.Id,
            CertificateNumber: c.CertificateNumber,
            CalibrationRecordId: c.CalibrationRecordId,
            EquipmentId: c.CalibrationRecord?.EquipmentId ?? Guid.Empty,
            EquipmentTag: c.CalibrationRecord?.Equipment?.LabTag ?? string.Empty,
            EquipmentName: c.CalibrationRecord?.Equipment?.Name ?? string.Empty,
            ClientName: c.CalibrationRecord?.Equipment?.Client?.Name ?? string.Empty,
            Status: c.Status,
            CalibrationDate: c.CalibrationRecord?.CalibrationDate ?? c.CreatedAt,
            IssuedAt: c.IssuedAt,
            PdfFileName: c.PdfFileName,
            HasPdf: c.PdfContent != null && c.PdfContent.Length > 0
        )).ToList();
    }
}
