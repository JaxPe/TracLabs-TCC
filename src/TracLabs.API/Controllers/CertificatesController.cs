using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TracLabs.Application.Constants;
using TracLabs.Application.Features.Certificates.Commands.GenerateCertificatePdf;
using TracLabs.Application.Features.Certificates.Commands.IssueCertificate;
using TracLabs.Application.Features.Certificates.Commands.SupersedeCertificate;
using TracLabs.Application.Features.Certificates.DTOs;
using TracLabs.Application.Features.Certificates.Queries.DownloadCertificatePdf;
using TracLabs.Application.Features.Certificates.Queries.GetCertificateById;
using TracLabs.Application.Features.Certificates.Queries.GetCertificateByNumber;
using TracLabs.Application.Features.Certificates.Queries.GetCertificatesByClient;
using TracLabs.Application.Features.Certificates.Queries.GetCertificatesByEquipment;
using TracLabs.Application.Features.Certificates.Queries.PreviewCertificatePdf;

namespace TracLabs.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CertificatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CertificatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Consulta os detalhes e status metrológico de um certificado pelo ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CertificateDetailDto>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetCertificateByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Localiza um certificado pelo seu número formal sequencial (ex.: CERT-2026-00001).
    /// </summary>
    [HttpGet("by-number/{certificateNumber}")]
    public async Task<ActionResult<CertificateDetailDto>> GetByNumber(string certificateNumber)
    {
        var result = await _mediator.Send(new GetCertificateByNumberQuery(certificateNumber));
        return Ok(result);
    }

    /// <summary>
    /// Lista todos os certificados associados ao histórico de um equipamento específico.
    /// </summary>
    [HttpGet("equipment/{equipmentId:guid}")]
    public async Task<ActionResult<IReadOnlyList<CertificateSummaryDto>>> GetByEquipmentId(Guid equipmentId)
    {
        var result = await _mediator.Send(new GetCertificatesByEquipmentQuery(equipmentId));
        return Ok(result);
    }

    /// <summary>
    /// Lista todos os certificados emitidos para um cliente específico.
    /// </summary>
    [HttpGet("client/{clientId:guid}")]
    public async Task<ActionResult<IReadOnlyList<CertificateSummaryDto>>> GetByClientId(Guid clientId)
    {
        var result = await _mediator.Send(new GetCertificatesByClientQuery(clientId));
        return Ok(result);
    }

    /// <summary>
    /// Download do arquivo PDF oficial salvo para um certificado emitido ou em rascunho.
    /// </summary>
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var (content, fileName) = await _mediator.Send(new DownloadCertificatePdfQuery(id));
        return File(content, "application/pdf", fileName);
    }

    /// <summary>
    /// Pré-visualização em tempo real do PDF com opções customizáveis (cores, logo, seções e notas).
    /// Não persiste alterações no banco de dados.
    /// </summary>
    [HttpPost("{id:guid}/preview-pdf")]
    public async Task<IActionResult> PreviewPdf(Guid id, [FromBody] CertificateCustomizationOptions? customization)
    {
        var (content, fileName) = await _mediator.Send(new PreviewCertificatePdfQuery(id, customization));
        return File(content, "application/pdf", fileName);
    }

    /// <summary>
    /// Gera e anexa o documento PDF oficial ao certificado utilizando o motor QuestPDF
    /// com base nas opções visuais configuradas.
    /// </summary>
    [HttpPost("{id:guid}/generate-pdf")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<CertificateDetailDto>> GeneratePdf(
        Guid id, [FromBody] CertificateCustomizationOptions? customization)
    {
        var result = await _mediator.Send(new GenerateCertificatePdfCommand(id, customization));
        return Ok(result);
    }

    /// <summary>
    /// Emissão formal do certificado (restrito a Supervisor).
    /// Valida anexação do PDF, transiciona o certificado para 'Issued' e o equipamento para 'CertificateIssued'.
    /// </summary>
    [HttpPost("{id:guid}/issue")]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<CertificateDetailDto>> Issue(Guid id)
    {
        var result = await _mediator.Send(new IssueCertificateCommand(id));
        return Ok(result);
    }

    /// <summary>
    /// Cancela ou substitui formalmente um certificado emitido com registro de justificativa (restrito a Supervisor).
    /// </summary>
    [HttpPost("{id:guid}/supersede")]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<CertificateDetailDto>> Supersede(
        Guid id, [FromBody] SupersedeCertificateRequest request)
    {
        var result = await _mediator.Send(new SupersedeCertificateCommand(id, request.Reason));
        return Ok(result);
    }
}

public record SupersedeCertificateRequest(string Reason);
