using HelpDesk_Flow.Enums;
using HelpDesk_Flow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk_Flow.Data;

public static class DevelopmentData
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var password = configuration["Demo:Password"];
        if (string.IsNullOrWhiteSpace(password))
            return;

        await using var transaction = await db.Database.BeginTransactionAsync();
        // Serializa a carga local mesmo quando dois processos iniciam ao mesmo tempo.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(634118)");
        var adminId = Guid.Parse("da623eef-141f-4c71-92ba-415fc4552171");
        if (await db.Users.AnyAsync(u => u.Id == adminId))
            return;

        var hasher = services.GetRequiredService<IPasswordHasher<User>>();
        var now = DateTime.UtcNow;
        User Account(Guid id, string name, string email, UserRole role)
        {
            var user = new User { Id = id, Name = name, Email = email, Role = role, CreatedAt = now, UpdatedAt = now };
            user.PasswordHash = hasher.HashPassword(user, password);
            return user;
        }

        var admin = Account(adminId, "Marina Costa", "admin@helpdesk.test", UserRole.Admin);
        var technician = Account(Guid.Parse("0bb8739c-75aa-4a12-819e-17819a65bca1"),
            "Rafael Lima", "tecnico@helpdesk.test", UserRole.Technician);
        var requester = Account(Guid.Parse("0e15a8bb-5c61-47f4-a5b7-2e78ac865df6"),
            "Ana Ribeiro", "usuario@helpdesk.test", UserRole.User);
        db.Users.AddRange(admin, technician, requester);

        var samples = new (string Title, string Description, TicketStatus Status, TicketPriority Priority, User Requester, User? Assignee)[]
        {
            ("VPN desconecta ao acessar o financeiro", "A conexão cai ao abrir o sistema financeiro. O problema começou hoje, após a atualização do notebook.", TicketStatus.Open, TicketPriority.Critical, requester, null),
            ("Acesso ao drive da equipe comercial", "Preciso consultar a pasta de propostas para preparar a reunião de quinta-feira.", TicketStatus.InProgress, TicketPriority.High, requester, technician),
            ("Impressora do 2º andar fora da rede", "A impressora aparece como offline nos computadores da sala de operações.", TicketStatus.Open, TicketPriority.Medium, technician, null),
            ("Configurar e-mail no celular corporativo", "Troquei de aparelho e preciso adicionar a conta de trabalho.", TicketStatus.Resolved, TicketPriority.Low, requester, technician),
            ("Licença de planilhas para novo colaborador", "O novo integrante do time de compras precisa da licença para finalizar a configuração.", TicketStatus.Open, TicketPriority.Medium, admin, null),
            ("Áudio baixo na sala de reuniões", "O volume do microfone da sala 03 está muito baixo em chamadas externas.", TicketStatus.InProgress, TicketPriority.Low, requester, technician),
            ("Redefinição de acesso ao portal interno", "O portal não reconhecia o segundo fator após a troca de aparelho.", TicketStatus.Closed, TicketPriority.Medium, requester, technician)
        };

        for (var i = 0; i < samples.Length; i++)
        {
            var (title, description, status, priority, author, assignee) = samples[i];
            var updated = now.AddHours(-2 * i - 1);
            var ticket = new Ticket
            {
                Title = title, Description = description, Status = status, Priority = priority,
                Requester = author, AssignedTo = assignee, CreatedAt = updated.AddHours(-1), UpdatedAt = updated,
                ResolvedAt = status is TicketStatus.Resolved or TicketStatus.Closed ? updated.AddMinutes(-10) : null,
                ClosedAt = status == TicketStatus.Closed ? updated : null
            };
            if (i == 1)
                ticket.Comments.Add(new Comment { Author = technician, Content = "Solicitação encaminhada à equipe responsável pela pasta.", CreatedAt = updated });
            db.Tickets.Add(ticket);
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
