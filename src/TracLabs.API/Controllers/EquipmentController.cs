using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TracLabs.Application.Constants;
using TracLabs.Application.Features.Equipment.Commands.AddMaintenance;
using TracLabs.Application.Features.Equipment.Commands.CreateEquipment;
using TracLabs.Application.Features.Equipment.Commands.ReportOccurrence;
using TracLabs.Application.Features.Equipment.Commands.UpdateEquipment;
using TracLabs.Application.Features.Equipment.Commands.UpdateLocation;
using TracLabs.Application.Features.Equipment.DTOs;
using TracLabs.Application.Features.Equipment.Queries.GetEquipmentById;
using TracLabs.Application.Features.Equipment.Queries.GetEquipmentHistory;
using TracLabs.Application.Features.Equipment.Queries.SearchEquipment;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Domain.Enums;

namespace TracLabs.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class EquipmentController : ControllerBase
{
    private readonly IMediator _mediator;

    public EquipmentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Busca avançada de equipamentos com múltiplos filtros opcionais.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<IReadOnlyList<EquipmentSummaryDto>>> Search(
        [FromQuery] string? labTag,
        [FromQuery] string? serialNumber,
        [FromQuery] string? model,
        [FromQuery] string? clientName,
        [FromQuery] EquipmentStatus? status)
    {
        var result = await _mediator.Send(new SearchEquipmentQuery(labTag, serialNumber, model, clientName, status));
        return Ok(result);
    }

    /// <summary>Obtém os detalhes de um equipamento por ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<EquipmentDetailDto>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetEquipmentByIdQuery(id));
        return Ok(result);
    }

    /// <summary>Obtém o histórico completo do equipamento: calibrações, manutenções e ocorrências/danos.</summary>
    [HttpGet("{id:guid}/history")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<EquipmentHistoryDto>> GetHistory(Guid id)
    {
        var result = await _mediator.Send(new GetEquipmentHistoryQuery(id));
        return Ok(result);
    }

    /// <summary>Cadastra um novo instrumento de cliente (com registros de conformidade e localização da ISO 17025).</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<EquipmentDetailDto>> Create([FromBody] CreateEquipmentCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Atualiza as informações cadastrais do equipamento de cliente (correção de erros ou mudanças técnicas).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<EquipmentDetailDto>> Update(Guid id, [FromBody] UpdateEquipmentCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { message = "O ID da rota não corresponde ao ID do corpo da requisição." });

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Atualiza a localização física atual do equipamento no laboratório (ISO 17025 6.4.13 d).</summary>
    [HttpPatch("{id:guid}/location")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<EquipmentDetailDto>> UpdateLocation(Guid id, [FromBody] UpdateLocationRequest request)
    {
        var result = await _mediator.Send(new UpdateEquipmentLocationCommand(id, request.NewLocation));
        return Ok(result);
    }

    /// <summary>Registra manutenção ou limpeza técnica no equipamento (ISO 17025 6.4.13 g).</summary>
    [HttpPost("{id:guid}/maintenance")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<MaintenanceRecordDto>> AddMaintenance(Guid id, [FromBody] AddEquipmentMaintenanceCommand command)
    {
        if (id != command.EquipmentId)
            return BadRequest(new { message = "O ID da rota não corresponde ao EquipmentId do corpo." });

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Registra ocorrência, avaria na chegada ou reparo no equipamento (ISO 17025 6.4.13 h / 7.4.3).</summary>
    [HttpPost("{id:guid}/occurrences")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<OccurrenceRecordDto>> ReportOccurrence(Guid id, [FromBody] ReportEquipmentOccurrenceCommand command)
    {
        if (id != command.EquipmentId)
            return BadRequest(new { message = "O ID da rota não corresponde ao EquipmentId do corpo." });

        var result = await _mediator.Send(command);
        return Ok(result);
    }
}

public record UpdateLocationRequest(string NewLocation);

