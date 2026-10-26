using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using PrismaSlice.Application.Abstractions;
using PrismaSlice.Application.Tickets;
using PrismaSlice.Domain.Entities;

namespace PrismaSlice.Infrastructure.Persistence;

public sealed class TicketRepository(AppDbContext db) : ITicketRepository
{
    public Task<bool> UserExistsAsync(int userId, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.Id == userId, ct);

    public Task<bool> TeamExistsAsync(int teamId, CancellationToken ct = default) =>
        db.Teams.AnyAsync(t => t.Id == teamId, ct);

    public async Task<IReadOnlyList<Label>> GetLabelsAsync(
        IReadOnlyCollection<string> names, CancellationToken ct = default) =>
        names.Count == 0
            ? []
            : await db.Labels.Where(l => names.Contains(l.Name)).ToListAsync(ct);

    public async Task AddAsync(Ticket ticket, CancellationToken ct = default)
    {
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);
    }

    public Task<TicketDetails?> GetDetailsAsync(string code, CancellationToken ct = default) =>
        db.Tickets
            .AsNoTracking()
            .Where(t => t.Code == code)
            .Select(t => new TicketDetails(
                t.Code,
                t.Title,
                t.Description,
                t.Category,
                t.Status,
                t.Priority,
                t.IsConfidential,
                t.Requester.Name,
                t.Assignee != null ? t.Assignee.Name : null,
                t.Team != null ? t.Team.Name : null,
                t.Labels.OrderBy(l => l.Name).Select(l => l.Name).ToList(),
                t.CreatedAt,
                t.ResolvedAt,
                t.Embedding != null))
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<TicketSearchHit>> SearchAsync(
        float[] queryEmbedding, int limit, CancellationToken ct = default)
    {
        var query = new Vector(queryEmbedding);

        // ORDER BY embedding <=> @query LIMIT n: la forma que aprovecha el índice HNSW.
        return await db.Tickets
            .AsNoTracking()
            .Where(t => t.Embedding != null && !t.IsConfidential)
            .OrderBy(t => t.Embedding!.CosineDistance(query))
            .Take(limit)
            .Select(t => new TicketSearchHit(
                t.Code,
                t.Title,
                t.Category,
                t.Status,
                t.Priority,
                1 - t.Embedding!.CosineDistance(query)))
            .ToListAsync(ct);
    }
}
