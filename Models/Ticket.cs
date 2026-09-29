using HelpDesk_Flow.Enums;

namespace HelpDesk_Flow.Models
{
    public class Ticket
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TicketStatus Status { get; set; }
        public TicketPriority Priority { get; set; }

        public Guid RequesterId { get; set; }
        public User Requester { get; set; }

        public Guid? AssignedToId { get; set; }
        public User? AssignedTo { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
