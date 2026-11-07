using HelpDesk_Flow.DTOs;
using HelpDesk_Flow.Models;
using HelpDesk_Flow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HelpDesk_Flow.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("register"), EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto input, CancellationToken ct)
        => StatusCode(201, await auth.RegisterAsync(input, ct));

    [HttpPost("login"), EnableRateLimiting("auth")]
    public Task<AuthResponseDto> Login(LoginDto input, CancellationToken ct) => auth.LoginAsync(input, ct);

    [Authorize, HttpGet("me")]
    public UserDto Me() => UserDto.From((User)HttpContext.Items[typeof(User)]!);
}
