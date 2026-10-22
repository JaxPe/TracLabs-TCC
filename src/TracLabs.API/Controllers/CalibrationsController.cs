using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TracLabs.Application.Constants;
using TracLabs.Application.DTOs.Calibration;
using TracLabs.Application.Features.Calibrations.Commands.CalculateVolume;
using TracLabs.Application.Features.Calibrations.Commands.CreateVolumeCalibration;
using TracLabs.Application.Features.Calibrations.DTOs;
using TracLabs.Application.Features.Calibrations.Queries.GetCalibrationById;
using TracLabs.Application.Features.Calibrations.Queries.GetCalibrationsByDateRange;
using TracLabs.Application.Features.Calibrations.Queries.GetCalibrationsByEquipment;
using TracLabs.Application.Features.Equipment.DTOs;

namespace TracLabs.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalibrationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CalibrationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Simulação prévia: Executa o cálculo metrológico completo de calibração volumétrica (DOC-CGCRE-027)
    /// e retorna o orçamento de incerteza em tempo real sem persistir no banco de dados.
    /// </summary>
    [HttpPost("volume/calculate")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<VolumeCalibrationResultDto>> CalculateVolume([FromBody] VolumeCalibrationRequest request)
    {
        var result = await _mediator.Send(new CalculateVolumeCalibrationCommand(request));
        return Ok(result);
    }

    /// <summary>
    /// Efetivação: Valida os requisitos do padrão e do instrumento, executa o cálculo metrológico,
    /// vincula o técnico logado, persiste o registro de calibração e transita o status para Calibrated.
    /// </summary>
    [HttpPost("volume")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<CalibrationRecordDto>> CreateVolumeCalibration([FromBody] CreateVolumeCalibrationCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Obtém um registro de calibração completo por ID (pontos, incertezas e responsável).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<CalibrationRecordDto>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetCalibrationByIdQuery(id));
        return Ok(result);
    }

    /// <summary>Lista o histórico de todas as calibrações de um instrumento de cliente específico.</summary>
    [HttpGet("equipment/{equipmentId:guid}")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<IReadOnlyList<CalibrationRecordSummaryDto>>> GetByEquipment(Guid equipmentId)
    {
        var result = await _mediator.Send(new GetCalibrationsByEquipmentQuery(equipmentId));
        return Ok(result);
    }

    /// <summary>Relatório gerencial de calibrações realizadas por período (restrito a Supervisores).</summary>
    [HttpGet("date-range")]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<IReadOnlyList<CalibrationRecordSummaryDto>>> GetByDateRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var result = await _mediator.Send(new GetCalibrationsByDateRangeQuery(from, to));
        return Ok(result);
    }
}
