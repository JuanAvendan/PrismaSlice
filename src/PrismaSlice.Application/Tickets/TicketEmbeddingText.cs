using PrismaSlice.Domain.Entities;

namespace PrismaSlice.Application.Tickets;

/// <summary>
/// Texto que se embebe por ticket. Un solo lugar para que la API y el seed
/// indexen exactamente lo mismo.
/// </summary>
public static class TicketEmbeddingText
{
    public static string For(Ticket ticket) =>
        $"{ticket.Title}\n{ticket.Category}\n{ticket.Description}";
}
