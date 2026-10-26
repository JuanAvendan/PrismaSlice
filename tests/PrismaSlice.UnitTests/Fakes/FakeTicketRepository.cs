using PrismaSlice.Application.Abstractions;
using PrismaSlice.Application.Tickets;
using PrismaSlice.Domain.Entities;

namespace PrismaSlice.UnitTests.Fakes;

internal sealed class FakeTicketRepository : ITicketRepository
{
    public HashSet<int> UserIds { get; } = [1];
    public HashSet<int> TeamIds { get; } = [1];
    public List<Label> Labels { get; } = [new() { Id = 1, Name = "bug" }];
    public List<Ticket> Saved { get; } = [];
    public (float[] Embedding, int Limit)? LastSearch { get; private set; }

    public Task<bool> UserExistsAsync(int userId, CancellationToken ct = default) =>
        Task.FromResult(UserIds.Contains(userId));

    public Task<bool> TeamExistsAsync(int teamId, CancellationToken ct = default) =>
        Task.FromResult(TeamIds.Contains(teamId));

    public Task<IReadOnlyList<Label>> GetLabelsAsync(
        IReadOnlyCollection<string> names, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Label>>(Labels.Where(l => names.Contains(l.Name)).ToList());

    public Task AddAsync(Ticket ticket, CancellationToken ct = default)
    {
        ticket.Code = $"TCK-{Saved.Count + 1:00000}";
        Saved.Add(ticket);
        return Task.CompletedTask;
    }

    public Task<TicketDetails?> GetDetailsAsync(string code, CancellationToken ct = default) =>
        Task.FromResult(Saved
            .Where(t => t.Code == code)
            .Select(t => new TicketDetails(
                t.Code, t.Title, t.Description, t.Category, t.Status, t.Priority,
                t.IsConfidential, "Solicitante", null, null,
                t.Labels.Select(l => l.Name).ToList(), t.CreatedAt, t.ResolvedAt,
                t.Embedding is not null))
            .SingleOrDefault());

    public Task<IReadOnlyList<TicketSearchHit>> SearchAsync(
        float[] queryEmbedding, int limit, CancellationToken ct = default)
    {
        LastSearch = (queryEmbedding, limit);
        return Task.FromResult<IReadOnlyList<TicketSearchHit>>([]);
    }
}
