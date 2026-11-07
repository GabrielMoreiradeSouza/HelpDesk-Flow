using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HelpDesk_Flow.Data;
using HelpDesk_Flow.DTOs;
using HelpDesk_Flow.Enums;
using HelpDesk_Flow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace HelpDesk_Flow.Services;

public sealed class AuthService(AppDbContext db, IPasswordHasher<User> passwords, JwtSettings jwt)
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto input, CancellationToken ct)
    {
        var user = new User
        {
            Name = input.Name.Trim(),
            Email = input.Email.Trim().ToLowerInvariant(),
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = passwords.HashPassword(user, input.Password);
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_users_email" })
        {
            throw new ApiException(409, "Este e-mail já está cadastrado.");
        }

        return IssueToken(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto input, CancellationToken ct)
    {
        var email = input.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !user.IsActive)
            throw new ApiException(401, "E-mail ou senha inválidos.");

        var result = passwords.VerifyHashedPassword(user, user.PasswordHash, input.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new ApiException(401, "E-mail ou senha inválidos.");

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwords.HashPassword(user, input.Password);
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return IssueToken(user);
    }

    private AuthResponseDto IssueToken(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(jwt.ExpirationMinutes);
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", user.Role.ToString())
        ];
        var token = new JwtSecurityToken(jwt.Issuer, jwt.Audience, claims, expires: expires,
            signingCredentials: new SigningCredentials(jwt.Key, SecurityAlgorithms.HmacSha256));

        return new(new JwtSecurityTokenHandler().WriteToken(token), expires, UserDto.From(user));
    }
}
