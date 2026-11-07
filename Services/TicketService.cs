using System.Data;
using HelpDesk_Flow.Data;
using HelpDesk_Flow.DTOs;
using HelpDesk_Flow.Enums;
using HelpDesk_Flow.Models;
using HelpDesk_Flow.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HelpDesk_Flow.Services;

public sealed class TicketService(TicketRepository tickets, AppDbContext db, ILogger<TicketService> logger)
{
    public Task<PagedResponse<TicketDto>> ListAsync(User actor, TicketQuery filters, CancellationToken ct)
        => tickets.ListAsync(actor, filters, ct);

    public async Task<TicketDto> ReadAsync(User actor, Guid id, CancellationToken ct)
        => await tickets.ReadAsync(actor, id, ct) ?? throw NotFound();

    public async Task<TicketDto> CreateAsync(User actor, CreateTicketDto input, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            Title = input.Title.Trim(), Description = input.Description.Trim(), Priority = input.Priority,
            RequesterId = actor.Id, CreatedAt = now, UpdatedAt = now
        };
        tickets.Add(ticket);
        await tickets.SaveAsync(ct);
        logger.LogInformation("Ticket {TicketId} criado por {UserId}.", ticket.Id, actor.Id);
        return await ReadAsync(actor, ticket.Id, ct);
    }

    public async Task<TicketDto> UpdateAsync(User actor, Guid id, UpdateTicketDto input, CancellationToken ct)
    {
        var ticket = await FindAsync(actor, id, ct);
        var canEditDetails = ticket.RequesterId == actor.Id || actor.Role == UserRole.Admin;
        if (!canEditDetails && (input.Title is not null || input.Description is not null || actor.Role != UserRole.Technician))
            throw new ApiException(403, "Você não tem permissão para editar este chamado.");

        EnsureEditable(ticket);
        if (input.Title is not null)
            ticket.Title = RequiredText(input.Title);
        if (input.Description is not null)
            ticket.Description = RequiredText(input.Description);
        if (input.Priority is { } priority)
            ticket.Priority = priority;
        await SaveChangeAsync(ticket, ct);
        return await ReadAsync(actor, id, ct);
    }

    public async Task<TicketDto> ChangeStatusAsync(User actor, Guid id, TicketStatus next, CancellationToken ct)
    {
        var ticket = await FindAsync(actor, id, ct);
        if (ticket.Status == next)
            return await ReadAsync(actor, id, ct);

        var allowed = (ticket.Status, next) switch
        {
            (TicketStatus.Open, TicketStatus.InProgress or TicketStatus.Cancelled) => true,
            (TicketStatus.InProgress, TicketStatus.Resolved or TicketStatus.Cancelled) => true,
            (TicketStatus.Resolved, TicketStatus.InProgress or TicketStatus.Closed or TicketStatus.Cancelled) => true,
            _ => false
        };
        if (!allowed)
            throw new ApiException(409, "Essa mudança de situação não é permitida.");

        ticket.Status = next;
        if (next == TicketStatus.Resolved)
            ticket.ResolvedAt = DateTime.UtcNow;
        if (next == TicketStatus.InProgress)
            ticket.ResolvedAt = null;
        if (next == TicketStatus.Closed)
            ticket.ClosedAt = DateTime.UtcNow;

        await SaveChangeAsync(ticket, ct);
        logger.LogInformation("Ticket {TicketId} movido para {Status} por {UserId}.", id, next, actor.Id);
        return await ReadAsync(actor, id, ct);
    }

    public async Task<TicketDto> AssignAsync(User actor, Guid id, Guid technicianId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var ticket = await FindAsync(actor, id, ct);
        EnsureEditable(ticket);
        if (!await tickets.CanAssignAsync(technicianId, ct))
            throw new ApiException(422, "Selecione um técnico ou administrador ativo.");

        ticket.AssignedToId = technicianId;
        try
        {
            await SaveChangeAsync(ticket, ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception error) when ((error is DbUpdateException ? error.InnerException : error) is PostgresException
            { SqlState: PostgresErrorCodes.SerializationFailure })
        {
            throw new ApiException(409, "Os dados foram alterados em outra sessão. Atualize e tente novamente.");
        }
        logger.LogInformation("Ticket {TicketId} atribuído a {TechnicianId} por {UserId}.", id, technicianId, actor.Id);
        return await ReadAsync(actor, id, ct);
    }

    public async Task<TicketDto> TakeAsync(User actor, Guid id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var ticket = await FindAsync(actor, id, ct);
        EnsureEditable(ticket);

        if (ticket.AssignedToId == actor.Id)
            return await ReadAsync(actor, id, ct);
        if (ticket.AssignedToId is not null)
            throw new ApiException(409, "Este chamado já possui um responsável.");

        ticket.AssignedToId = actor.Id;
        try
        {
            await SaveChangeAsync(ticket, ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception error) when ((error is DbUpdateException ? error.InnerException : error) is PostgresException
            { SqlState: PostgresErrorCodes.SerializationFailure })
        {
            throw new ApiException(409, "O chamado foi atualizado por outra pessoa. Atualize e tente novamente.");
        }

        logger.LogInformation("Ticket {TicketId} pego pelo técnico {TechnicianId}.", id, actor.Id);
        return await ReadAsync(actor, id, ct);
    }

    public async Task<IReadOnlyList<CommentDto>> CommentsAsync(User actor, Guid id, CancellationToken ct)
    {
        await FindAsync(actor, id, ct);
        return await tickets.CommentsAsync(id, ct);
    }

    public async Task<CommentDto> CommentAsync(User actor, Guid id, CreateCommentDto input, CancellationToken ct)
    {
        var ticket = await FindAsync(actor, id, ct);
        EnsureEditable(ticket);
        var comment = new Comment
        {
            TicketId = id, AuthorId = actor.Id, Content = input.Content.Trim(), CreatedAt = DateTime.UtcNow
        };
        tickets.Add(comment);
        await SaveChangeAsync(ticket, ct);
        return new(comment.Id, new(actor.Id, actor.Name), comment.Content, comment.CreatedAt, null);
    }

    private async Task<Ticket> FindAsync(User actor, Guid id, CancellationToken ct) =>
        await tickets.FindAsync(actor, id, ct) ?? throw NotFound();

    private async Task SaveChangeAsync(Ticket ticket, CancellationToken ct)
    {
        ticket.UpdatedAt = DateTime.UtcNow;
        await tickets.SaveAsync(ct);
    }

    private static ApiException NotFound() => new(404, "Chamado não encontrado.");

    private static void EnsureEditable(Ticket ticket)
    {
        if (ticket.Status is TicketStatus.Closed or TicketStatus.Cancelled)
            throw new ApiException(409, "Chamados encerrados ou cancelados ficam disponíveis para consulta.");
    }

    private static string RequiredText(string value) => string.IsNullOrWhiteSpace(value)
        ? throw new ApiException(400, "Título e descrição devem conter texto.")
        : value.Trim();
}
