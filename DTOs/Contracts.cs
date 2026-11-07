using System.ComponentModel.DataAnnotations;
using HelpDesk_Flow.Enums;
using HelpDesk_Flow.Models;

namespace HelpDesk_Flow.DTOs;

public record PersonDto(Guid Id, string Name);
public record UserDto(Guid Id, string Name, string Email, UserRole Role, bool IsActive)
{
    public static UserDto From(User user) => new(user.Id, user.Name, user.Email, user.Role, user.IsActive);
}
public record AuthResponseDto(string AccessToken, DateTime ExpiresAt, UserDto User);
public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);

public record TicketDto(Guid Id, string Title, string Description, TicketStatus Status, TicketPriority Priority,
    PersonDto Requester, PersonDto? AssignedTo, DateTime CreatedAt, DateTime UpdatedAt,
    DateTime? ResolvedAt, DateTime? ClosedAt, int CommentCount);

public record CommentDto(Guid Id, PersonDto Author, string Content, DateTime CreatedAt, DateTime? UpdatedAt);
public record CreateCommentDto([Required, StringLength(4000)] string Content);
public record AssignTicketDto([Required] Guid? TechnicianId);
public record UpdateTicketStatusDto([Required] TicketStatus? Status);
public record UpdateUserRoleDto([Required] UserRole? Role);

public class TicketQuery
{
    [Range(1, 1000000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    [StringLength(160)] public string? Search { get; init; }
    public TicketStatus? Status { get; init; }
    public TicketPriority? Priority { get; init; }
    public bool Mine { get; init; }
}
