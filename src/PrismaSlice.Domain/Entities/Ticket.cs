using Pgvector;
using PrismaSlice.Domain.Enums;

namespace PrismaSlice.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;   // legible: "TCK-00042"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public bool IsConfidential { get; set; }

    // Solicitante (obligatorio) y agente asignado (opcional): dos FKs a User.
    public int RequesterId { get; set; }
    public User Requester { get; set; } = null!;
    public int? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    // Equipo dueño del ticket (opcional).
    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    // Embedding semántico de Title + Description. Null hasta que el pipeline lo genere.
    public Vector? Embedding { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Label> Labels { get; set; } = new List<Label>();
}