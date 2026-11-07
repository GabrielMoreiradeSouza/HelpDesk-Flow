using System.Linq.Expressions;
using HelpDesk_Flow.Data;
using HelpDesk_Flow.DTOs;
using HelpDesk_Flow.Enums;
using HelpDesk_Flow.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk_Flow.Repositories;

public sealed class TicketRepository(AppDbContext db)
{
    private static readonly Expression<Func<Ticket, TicketDto>> Projection = ticket => new(
        ticket.Id, ticket.Title, ticket.Description, ticket.Status, ticket.Priority,
        new(ticket.RequesterId, ticket.Requester.Name),
        ticket.AssignedTo == null ? null : new(ticket.AssignedTo.Id, ticket.AssignedTo.Name),
        ticket.CreatedAt, ticket.UpdatedAt, ticket.ResolvedAt, ticket.ClosedAt, ticket.Comments.Count);

    private IQueryable<Ticket> VisibleTo(User actor) => actor.Role == UserRole.User
        ? db.Tickets.Where(t => t.RequesterId == actor.Id)
        : db.Tickets;

    public async Task<PagedResponse<TicketDto>> ListAsync(User actor, TicketQuery filters, CancellationToken ct)
    {
        var query = VisibleTo(actor).AsNoTracking();
        if (filters.Mine)
            query = query.Where(t => t.RequesterId == actor.Id || t.AssignedToId == actor.Id);
        if (filters.Status is { } status)
            query = query.Where(t => t.Status == status);
        if (filters.Priority is { } priority)
            query = query.Where(t => t.Priority == priority);
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var pattern = $"%{search}%";
            var codePattern = search.StartsWith("HD-", StringComparison.OrdinalIgnoreCase) && search.Length > 3
                ? $"%{search[3..]}" : pattern;
            query = query.Where(t => EF.Functions.ILike(t.Title, pattern) ||
                EF.Functions.ILike(t.Description, pattern) ||
                EF.Functions.ILike(t.Requester.Name, pattern) ||
                EF.Functions.ILike(t.Id.ToString(), codePattern));
        }

        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(t => t.UpdatedAt).ThenByDescending(t => t.Id)
            .Skip((filters.Page - 1) * filters.PageSize).Take(filters.PageSize).Select(Projection).ToListAsync(ct);
        return new(items, filters.Page, filters.PageSize, count, (int)Math.Ceiling((double)count / filters.PageSize));
    }

    public Task<TicketDto?> ReadAsync(User actor, Guid id, CancellationToken ct) =>
        VisibleTo(actor).AsNoTracking().Where(t => t.Id == id).Select(Projection).SingleOrDefaultAsync(ct);

    public Task<Ticket?> FindAsync(User actor, Guid id, CancellationToken ct) =>
        VisibleTo(actor).SingleOrDefaultAsync(t => t.Id == id, ct);

    public void Add(Ticket ticket) => db.Tickets.Add(ticket);
    public void Add(Comment comment) => db.Comments.Add(comment);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public Task<List<CommentDto>> CommentsAsync(Guid id, CancellationToken ct) =>
        db.Comments.AsNoTracking().Where(c => c.TicketId == id).OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .Select(c => new CommentDto(c.Id, new(c.AuthorId, c.Author.Name), c.Content, c.CreatedAt, c.UpdatedAt))
            .ToListAsync(ct);

    public Task<bool> CanAssignAsync(Guid id, CancellationToken ct) =>
        db.Users.AnyAsync(u => u.Id == id && u.IsActive && (u.Role == UserRole.Technician || u.Role == UserRole.Admin), ct);
}
