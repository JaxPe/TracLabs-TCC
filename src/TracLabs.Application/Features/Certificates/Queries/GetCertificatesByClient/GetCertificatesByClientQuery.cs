using MediatR;
using TracLabs.Application.Features.Certificates.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Certificates.Queries.GetCertificatesByClient;

public record GetCertificatesByClientQuery(Guid ClientId) : IRequest<IReadOnlyList<CertificateSummaryDto>>;

public class GetCertificatesByClientQueryHandler : IRequestHandler<GetCertificatesByClientQuery, IReadOnlyList<CertificateSummaryDto>>
{
    private readonly ICertificateRepository _certificateRepository;

    public GetCertificatesByClientQueryHandler(ICertificateRepository certificateRepository)
    {
        _certificateRepository = certificateRepository;
    }

    public async Task<IReadOnlyList<CertificateSummaryDto>> Handle(GetCertificatesByClientQuery request, CancellationToken cancellationToken)
    {
        var certificates = await _certificateRepository.GetByClientIdAsync(request.ClientId, cancellationToken);

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
