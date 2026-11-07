using MediatR;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Certificates.Queries.DownloadCertificatePdf;

public record DownloadCertificatePdfQuery(
    Guid CertificateId
) : IRequest<(byte[] Content, string FileName)>;

public class DownloadCertificatePdfQueryHandler : IRequestHandler<DownloadCertificatePdfQuery, (byte[] Content, string FileName)>
{
    private readonly ICertificateRepository _certificateRepository;

    public DownloadCertificatePdfQueryHandler(ICertificateRepository certificateRepository)
    {
        _certificateRepository = certificateRepository;
    }

    public async Task<(byte[] Content, string FileName)> Handle(DownloadCertificatePdfQuery request, CancellationToken cancellationToken)
    {
        var cert = await _certificateRepository.GetByIdAsync(request.CertificateId, cancellationToken);
        if (cert == null)
            throw new KeyNotFoundException($"Certificado com ID '{request.CertificateId}' não encontrado.");

        if (cert.PdfContent == null || cert.PdfContent.Length == 0)
            throw new InvalidOperationException($"O PDF do certificado '{cert.CertificateNumber}' ainda não foi gerado.");

        var fileName = cert.PdfFileName ?? $"{cert.CertificateNumber}.pdf";
        return (cert.PdfContent, fileName);
    }
}
