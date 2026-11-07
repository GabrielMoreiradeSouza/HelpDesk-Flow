using System.ComponentModel.DataAnnotations;
using HelpDesk_Flow.Enums;

namespace HelpDesk_Flow.DTOs;

public record CreateTicketDto(
    [Required, StringLength(160)] string Title,
    [Required, StringLength(8000)] string Description,
    TicketPriority Priority = TicketPriority.Medium);
