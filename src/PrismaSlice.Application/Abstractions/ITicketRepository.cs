using PrismaSlice.Application.Tickets;
using PrismaSlice.Domain.Entities;

namespace PrismaSlice.Application.Abstractions;

public interface ITicketRepository
{
    Task<bool> UserExistsAsync(int userId, CancellationToken ct = default);
    Task<bool> TeamExistsAsync(int teamId, CancellationToken ct = default);
    Task<IReadOnlyList<Label>> GetLabelsAsync(
        IReadOnlyCollection<string> names, CancellationToken ct = default);

    /// <summary>Persiste el ticket; la base asigna <see cref="Ticket.Code"/>.</summary>
    Task AddAsync(Ticket ticket, CancellationToken ct = default);

    Task<TicketDetails?> GetDetailsAsync(string code, CancellationToken ct = default);

    /// <summary>
    /// Tickets no confidenciales más cercanos al embedding, por distancia coseno.
    /// </summary>
    Task<IReadOnlyList<TicketSearchHit>> SearchAsync(
        float[] queryEmbedding, int limit, CancellationToken ct = default);
}
