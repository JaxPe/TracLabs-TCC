using TracLabs.Application.Features.Certificates.DTOs;

namespace TracLabs.Application.Interfaces.Services;

/// <summary>
/// Contrato de serviço para geração de certificados de calibração em formato PDF.
/// Suporta customizações de layout preservando a integridade dos dados metrológicos (ISO/IEC 17025 item 7.8).
/// </summary>
public interface ICertificatePdfGenerator
{
    /// <summary>
    /// Renderiza o documento PDF para uma calibração gravimétrica de volume.
    /// </summary>
    /// <param name="reportData">Dados metrológicos, cadastrais e de rastreabilidade.</param>
    /// <param name="options">Opções de customização e estilo do layout.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Bytes do arquivo PDF gerado.</returns>
    Task<byte[]> GenerateVolumeCertificatePdfAsync(
        CertificateReportDto reportData,
        CertificateCustomizationOptions options,
        CancellationToken cancellationToken = default);
}
