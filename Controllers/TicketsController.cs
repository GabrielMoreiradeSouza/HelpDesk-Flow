using HelpDesk_Flow.DTOs;
using HelpDesk_Flow.Models;
using HelpDesk_Flow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk_Flow.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets")]
public sealed class TicketsController(TicketService tickets) : ControllerBase
{
    private User Actor => (User)HttpContext.Items[typeof(User)]!;

    [HttpGet]
    public Task<PagedResponse<TicketDto>> List([FromQuery] TicketQuery filters, CancellationToken ct)
        => tickets.ListAsync(Actor, filters, ct);

    [HttpGet("{id:guid}")]
    public Task<TicketDto> Read(Guid id, CancellationToken ct) => tickets.ReadAsync(Actor, id, ct);

    [HttpPost]
    public async Task<ActionResult<TicketDto>> Create(CreateTicketDto input, CancellationToken ct)
    {
        var ticket = await tickets.CreateAsync(Actor, input, ct);
        return CreatedAtAction(nameof(Read), new { id = ticket.Id }, ticket);
    }

    [HttpPut("{id:guid}")]
    public Task<TicketDto> Update(Guid id, UpdateTicketDto input, CancellationToken ct)
        => tickets.UpdateAsync(Actor, id, input, ct);

    [HttpPatch("{id:guid}/status"), Authorize(Roles = "Technician,Admin")]
    public Task<TicketDto> ChangeStatus(Guid id, UpdateTicketStatusDto input, CancellationToken ct)
        => tickets.ChangeStatusAsync(Actor, id, input.Status!.Value, ct);

    [HttpPatch("{id:guid}/assign"), Authorize(Roles = "Technician,Admin")]
    public Task<TicketDto> Assign(Guid id, AssignTicketDto input, CancellationToken ct)
        => tickets.AssignAsync(Actor, id, input.TechnicianId!.Value, ct);

    [HttpGet("{id:guid}/comments")]
    public Task<IReadOnlyList<CommentDto>> Comments(Guid id, CancellationToken ct) => tickets.CommentsAsync(Actor, id, ct);

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<CommentDto>> Comment(Guid id, CreateCommentDto input, CancellationToken ct)
        => StatusCode(201, await tickets.CommentAsync(Actor, id, input, ct));
}
