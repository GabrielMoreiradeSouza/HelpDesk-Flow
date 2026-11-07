using System.ComponentModel.DataAnnotations;

namespace HelpDesk_Flow.DTOs;

public record LoginDto(
    [Required, EmailAddress, StringLength(180)] string Email,
    [Required, StringLength(128)] string Password);
