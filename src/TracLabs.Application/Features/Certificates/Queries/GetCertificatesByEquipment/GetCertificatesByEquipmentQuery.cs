using MediatR;
using TracLabs.Application.Features.Certificates.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Certificates.Queries.GetCertificatesByEquipment;

public record GetCertificatesByEquipmentQuery(Guid EquipmentId) : IRequest<IReadOnlyList<CertificateSummaryDto>>;

public class GetCertificatesByEquipmentQueryHandler : IRequestHandler<GetCertificatesByEquipmentQuery, IReadOnlyList<CertificateSummaryDto>>
{
    private readonly ICertificateRepository _certificateRepository;

    public GetCertificatesByEquipmentQueryHandler(ICertificateRepository certificateRepository)
    {
        _certificateRepository = certificateRepository;
    }

    public async Task<IReadOnlyList<CertificateSummaryDto>> Handle(GetCertificatesByEquipmentQuery request, CancellationToken cancellationToken)
    {
        var certificates = await _certificateRepository.GetByEquipmentIdAsync(request.EquipmentId, cancellationToken);

        return certificates.Select(c => new CertificateSummaryDto(
            Id: c.Id,
            CertificateNumber: c.CertificateNumber,
            CalibrationRecordId: c.CalibrationRecordId,
            EquipmentId: c.CalibrationRecord?.EquipmentId ?? request.EquipmentId,
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
