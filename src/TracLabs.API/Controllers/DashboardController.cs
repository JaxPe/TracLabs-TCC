using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TracLabs.Application.Constants;
using TracLabs.Application.Features.Dashboard.DTOs;
using TracLabs.Application.Features.Dashboard.Queries.GetDashboardMetrics;
using TracLabs.Application.Features.Dashboard.Queries.GetRecentActivity;

namespace TracLabs.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Retorna os indicadores e métricas consolidadas do laboratório em tempo real.</summary>
    [HttpGet("metrics")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<DashboardMetricsDto>> GetMetrics()
    {
        var result = await _mediator.Send(new GetDashboardMetricsQuery());
        return Ok(result);
    }

    /// <summary>Retorna os eventos recentes e a linha do tempo operacional do laboratório.</summary>
    [HttpGet("recent-activity")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<IReadOnlyList<CalibrationTimelineEventDto>>> GetRecentActivity([FromQuery] int count = 10)
    {
        var result = await _mediator.Send(new GetRecentActivityQuery(count));
        return Ok(result);
    }
}
