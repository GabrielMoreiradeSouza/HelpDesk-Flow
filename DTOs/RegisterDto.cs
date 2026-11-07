using System.ComponentModel.DataAnnotations;

namespace HelpDesk_Flow.DTOs;

public record RegisterDto(
    [Required, StringLength(120)] string Name,
    [Required, EmailAddress, StringLength(180)] string Email,
    [Required, StringLength(128, MinimumLength = 8)] string Password);
