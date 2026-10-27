using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TracLabs.Application.Constants;
using TracLabs.Application.Features.Clients.Commands.CreateClient;
using TracLabs.Application.Features.Clients.Commands.DeleteClient;
using TracLabs.Application.Features.Clients.Commands.UpdateClient;
using TracLabs.Application.Features.Clients.DTOs;
using TracLabs.Application.Features.Clients.Queries.GetClientById;
using TracLabs.Application.Features.Clients.Queries.GetClients;
using TracLabs.Application.Features.Clients.Queries.GetClientWithEquipment;

namespace TracLabs.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Lista clientes com suporte a filtro opcional por nome.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<IReadOnlyList<ClientDto>>> GetAll([FromQuery] string? name)
    {
        var result = await _mediator.Send(new GetClientsQuery(name));
        return Ok(result);
    }

    /// <summary>Obtém os detalhes de um cliente por ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<ClientDto>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetClientByIdQuery(id));
        return Ok(result);
    }

    /// <summary>Obtém um cliente com a lista de todos os seus instrumentos cadastrados.</summary>
    [HttpGet("{id:guid}/equipment")]
    [Authorize(Roles = $"{Roles.Tecnico},{Roles.Supervisor}")]
    public async Task<ActionResult<ClientWithEquipmentDto>> GetWithEquipment(Guid id)
    {
        var result = await _mediator.Send(new GetClientWithEquipmentQuery(id));
        return Ok(result);
    }

    /// <summary>Cadastra um novo cliente (restrito a Supervisores).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<ClientDto>> Create([FromBody] CreateClientCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Atualiza dados cadastrais de um cliente (restrito a Supervisores).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<ClientDto>> Update(
        Guid id,
        [FromBody] UpdateClientCommand command
    )
    {
        if (id != command.Id)
            return BadRequest(
                new { message = "O ID da rota não corresponde ao ID do corpo da requisição." }
            );

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Desativa um cliente (Soft Delete — restrito a Supervisores).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteClientCommand(id));
        return NoContent();
    }
}

