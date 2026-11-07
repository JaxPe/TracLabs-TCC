using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TracLabs.Application.Constants;
using TracLabs.Application.Features.StandardEquipment.Commands.AddMaintenance;
using TracLabs.Application.Features.StandardEquipment.Commands.AddStandardCertificate;
using TracLabs.Application.Features.StandardEquipment.Commands.CreateStandardEquipment;
using TracLabs.Application.Features.StandardEquipment.Commands.PutOutOfService;
using TracLabs.Application.Features.StandardEquipment.Commands.ReactivateStandard;
using TracLabs.Application.Features.StandardEquipment.Commands.RegisterCalibrationResult;
using TracLabs.Application.Features.StandardEquipment.Commands.ReportOccurrence;
using TracLabs.Application.Features.StandardEquipment.Commands.SendToCalibration;
using TracLabs.Application.Features.StandardEquipment.Commands.UpdateStandardEquipment;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Features.StandardEquipment.Queries.GetExpiringStandards;
using TracLabs.Application.Features.StandardEquipment.Queries.GetStandardEquipment;
using TracLabs.Application.Features.StandardEquipment.Queries.GetStandardEquipmentById;
using TracLabs.Application.Features.StandardEquipment.Queries.GetUsableStandards;
using TracLabs.Domain.Enums;

namespace TracLabs.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class StandardEquipmentController : ControllerBase
{
    private readonly IMediator _mediator;

    public StandardEquipmentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Lista todos os equipamentos padrão com filtro opcional por status.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<IReadOnlyList<StandardEquipmentDto>>> GetAll([FromQuery] StandardEquipmentStatus? status)
    {
        var result = await _mediator.Send(new GetStandardEquipmentQuery(status));
        return Ok(result);
    }

    /// <summary>Lista exclusivamente os padrões aptos para uso em calibrações (Status Calibrated e dentro da validade).</summary>
    [HttpGet("usable")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<IReadOnlyList<StandardEquipmentDto>>> GetUsable()
    {
        var result = await _mediator.Send(new GetUsableStandardsQuery());
        return Ok(result);
    }

    /// <summary>Lista padrões cuja calibração expira nos próximos dias (alerta de vencimento).</summary>
    [HttpGet("expiring-soon")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<IReadOnlyList<StandardEquipmentDto>>> GetExpiringSoon([FromQuery] int withinDays = 30)
    {
        var result = await _mediator.Send(new GetExpiringStandardsQuery(withinDays));
        return Ok(result);
    }

    /// <summary>Obtém detalhes do equipamento padrão por ID, incluindo histórico de manutenções e ocorrências.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<StandardEquipmentDetailDto>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetStandardEquipmentByIdQuery(id));
        return Ok(result);
    }

    /// <summary>Cadastra um novo equipamento padrão do laboratório - Etapa 1: Ativo Físico (restrito a Supervisores).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<StandardEquipmentDto>> Create([FromBody] CreateStandardEquipmentCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Atualiza as informações cadastrais do equipamento padrão (restrito a Supervisores).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<StandardEquipmentDto>> Update(Guid id, [FromBody] UpdateStandardEquipmentCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { message = "O ID da rota não corresponde ao ID do corpo." });

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Cadastra um novo certificado de calibração metrológica com tabela de pontos e regressão linear - Etapa 2 (restrito a Supervisores).</summary>
    [HttpPost("{id:guid}/certificates")]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<StandardCertificateDto>> AddCertificate(Guid id, [FromBody] AddStandardCertificateCommand command)
    {
        if (id != command.StandardEquipmentId)
            return BadRequest(new { message = "O ID da rota não corresponde ao StandardEquipmentId do corpo da requisição." });

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Envia o padrão para calibração externa em laboratório acreditado RBC (status InCalibration).</summary>
    [HttpPost("{id:guid}/send-to-calibration")]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<StandardEquipmentDto>> SendToCalibration(Guid id, [FromBody] SendStandardToCalibrationCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { message = "O ID da rota não corresponde ao ID do corpo." });

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Registra o retorno e novo certificado de calibração do padrão (status Calibrated ou OutOfService).</summary>
    [HttpPost("{id:guid}/register-calibration")]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<StandardEquipmentDto>> RegisterCalibration(Guid id, [FromBody] RegisterStandardCalibrationResultCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { message = "O ID da rota não corresponde ao ID do corpo." });

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Coloca o equipamento padrão fora de serviço / quarentena (status OutOfService).</summary>
    [HttpPost("{id:guid}/out-of-service")]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<StandardEquipmentDto>> PutOutOfService(Guid id, [FromBody] PutStandardOutOfServiceCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { message = "O ID da rota não corresponde ao ID do corpo." });

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Reativa o equipamento padrão para uso após verificação/reparo.</summary>
    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<StandardEquipmentDto>> Reactivate(Guid id, [FromBody] ReactivateStandardCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { message = "O ID da rota não corresponde ao ID do corpo." });

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Registra uma manutenção preventiva ou corretiva no padrão (ISO 17025 6.4.13 g).</summary>
    [HttpPost("{id:guid}/maintenance")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<MaintenanceRecordDto>> AddMaintenance(Guid id, [FromBody] AddStandardMaintenanceCommand command)
    {
        if (id != command.StandardEquipmentId)
            return BadRequest(new { message = "O ID da rota não corresponde ao StandardEquipmentId do corpo." });

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Registra dano, mau funcionamento, modificação ou reparo no padrão (ISO 17025 6.4.13 h).</summary>
    [HttpPost("{id:guid}/occurrences")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<OccurrenceRecordDto>> ReportOccurrence(Guid id, [FromBody] ReportStandardOccurrenceCommand command)
    {
        if (id != command.StandardEquipmentId)
            return BadRequest(new { message = "O ID da rota não corresponde ao StandardEquipmentId do corpo." });

        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
