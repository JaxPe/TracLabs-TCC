using MediatR;
using TracLabs.Application.Features.Certificates.Common;
using TracLabs.Application.Features.Certificates.DTOs;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Application.Interfaces.Services;

namespace TracLabs.Application.Features.Certificates.Queries.PreviewCertificatePdf;

public record PreviewCertificatePdfQuery(
    Guid CertificateId,
    CertificateCustomizationOptions? Customization = null
) : IRequest<(byte[] Content, string FileName)>;

public class PreviewCertificatePdfQueryHandler : IRequestHandler<PreviewCertificatePdfQuery, (byte[] Content, string FileName)>
{
    private readonly ICertificateRepository _certificateRepository;
    private readonly ICertificatePdfGenerator _pdfGenerator;

    public PreviewCertificatePdfQueryHandler(
        ICertificateRepository certificateRepository,
        ICertificatePdfGenerator pdfGenerator)
    {
        _certificateRepository = certificateRepository;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<(byte[] Content, string FileName)> Handle(PreviewCertificatePdfQuery request, CancellationToken cancellationToken)
    {
        var cert = await _certificateRepository.GetWithDetailsAsync(request.CertificateId, cancellationToken);
        if (cert == null)
            throw new KeyNotFoundException($"Certificado com ID '{request.CertificateId}' não encontrado.");

        var options = request.Customization ?? new CertificateCustomizationOptions();
        var reportDto = CertificateReportBuilder.Build(cert);

        var pdfBytes = await _pdfGenerator.GenerateVolumeCertificatePdfAsync(reportDto, options, cancellationToken);
        var fileName = $"PREVIEW_{cert.CertificateNumber}.pdf";

        return (pdfBytes, fileName);
    }
}
