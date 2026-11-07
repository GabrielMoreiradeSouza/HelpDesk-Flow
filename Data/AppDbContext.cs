using System.Text.Json;
using HelpDesk_Flow.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk_Flow.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var users = modelBuilder.Entity<User>();
        users.ToTable("users");
        users.Property(u => u.Name).HasMaxLength(120);
        users.Property(u => u.Email).HasMaxLength(180);
        users.Property(u => u.Role).HasConversion<string>().HasMaxLength(30);
        users.Property(u => u.IsActive).HasDefaultValue(true);
        users.Property<uint>("xmin").IsRowVersion();
        users.HasIndex(u => u.Email).IsUnique();

        var tickets = modelBuilder.Entity<Ticket>();
        tickets.ToTable("tickets");
        tickets.Property(t => t.Title).HasMaxLength(160);
        tickets.Property(t => t.Status).HasConversion<string>().HasMaxLength(30);
        tickets.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);
        tickets.Property<uint>("xmin").IsRowVersion();
        tickets.HasIndex(t => t.Status);
        tickets.HasIndex(t => t.Priority);
        tickets.HasIndex(t => new { t.Status, t.Priority });
        tickets.HasIndex(t => t.CreatedAt);
        tickets.HasOne(t => t.Requester).WithMany(u => u.RequestedTickets)
            .HasForeignKey(t => t.RequesterId).OnDelete(DeleteBehavior.Restrict);
        tickets.HasOne(t => t.AssignedTo).WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssignedToId).OnDelete(DeleteBehavior.SetNull);

        var comments = modelBuilder.Entity<Comment>();
        comments.ToTable("comments");
        comments.HasIndex(c => new { c.TicketId, c.CreatedAt });
        comments.HasOne(c => c.Ticket).WithMany(t => t.Comments)
            .HasForeignKey(c => c.TicketId).OnDelete(DeleteBehavior.Cascade);
        comments.HasOne(c => c.Author).WithMany(u => u.Comments)
            .HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Restrict);

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
            property.SetColumnName(JsonNamingPolicy.SnakeCaseLower.ConvertName(property.Name));
    }
}
