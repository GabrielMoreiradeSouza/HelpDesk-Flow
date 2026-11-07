using System.Data;
using HelpDesk_Flow.Data;
using HelpDesk_Flow.DTOs;
using HelpDesk_Flow.Enums;
using HelpDesk_Flow.Models;
using HelpDesk_Flow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HelpDesk_Flow.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IReadOnlyList<UserDto>> List(CancellationToken ct) =>
        await db.Users.AsNoTracking().OrderBy(u => u.Name)
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Role, u.IsActive)).ToListAsync(ct);

    [HttpGet("technicians"), Authorize(Roles = "Admin,Technician")]
    public async Task<IReadOnlyList<PersonDto>> Technicians(CancellationToken ct) =>
        await db.Users.AsNoTracking().Where(u => u.IsActive && (u.Role == UserRole.Admin || u.Role == UserRole.Technician))
            .OrderBy(u => u.Name).Select(u => new PersonDto(u.Id, u.Name)).ToListAsync(ct);

    [HttpPatch("{id:guid}/role"), Authorize(Roles = "Admin")]
    public async Task<UserDto> UpdateRole(Guid id, UpdateUserRoleDto input, CancellationToken ct)
    {
        var actor = (User)HttpContext.Items[typeof(User)]!;
        if (actor.Id == id)
            throw new ApiException(409, "Você não pode alterar seu próprio perfil.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await db.Users.AnyAsync(u => u.Id == actor.Id && u.IsActive && u.Role == UserRole.Admin, ct))
            throw new ApiException(403, "Seu perfil atual não permite esta operação.");

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new ApiException(404, "Usuário não encontrado.");

        if (input.Role == UserRole.User && user.Role != UserRole.User &&
            await db.Tickets.AnyAsync(t => t.AssignedToId == id &&
                (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Resolved), ct))
            throw new ApiException(409, "Reatribua os chamados ativos antes de alterar este perfil.");

        user.Role = input.Role!.Value;
        user.UpdatedAt = DateTime.UtcNow;
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception error) when ((error is DbUpdateException ? error.InnerException : error) is PostgresException
            { SqlState: PostgresErrorCodes.SerializationFailure })
        {
            throw new ApiException(409, "Os dados foram alterados em outra sessão. Atualize e tente novamente.");
        }
        return UserDto.From(user);
    }
}
