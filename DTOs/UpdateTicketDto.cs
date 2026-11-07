using System.ComponentModel.DataAnnotations;
using HelpDesk_Flow.Enums;

namespace HelpDesk_Flow.DTOs;

public record UpdateTicketDto(
    [StringLength(160)] string? Title,
    [StringLength(8000)] string? Description,
    TicketPriority? Priority);
