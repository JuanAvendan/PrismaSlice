using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using PrismaSlice.Application.Abstractions;
using PrismaSlice.Application.Tickets;
using PrismaSlice.Domain.Entities;
using PrismaSlice.Domain.Enums;
using PrismaSlice.Infrastructure.Persistence;

namespace PrismaSlice.SeedData;

public sealed class Seeder(
    AppDbContext db,
    IEmbeddingService embeddings,
    TimeProvider time,
    ILogger<Seeder> logger)
{
    private const int EmbeddingBatchSize = 16;

    public async Task RunAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Aplicando migraciones…");
        await db.Database.MigrateAsync(ct);

        if (await db.Tickets.AnyAsync(ct))
            logger.LogInformation("La base ya tiene tickets; no se insertan datos de ejemplo.");
        else
            await SeedCatalogAsync(ct);

        await EmbedMissingAsync(ct);
    }

    private async Task SeedCatalogAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();

        var departments = SeedCatalog.Departments();

        var teams = SeedCatalog.TeamDefs
            .Select(d => new Team
            {
                Name = d.Name,
                Code = d.Code,
                Department = departments.Single(x => x.Code == d.DeptCode),
            })
            .ToList();

        var users = SeedCatalog.UserDefs
            .Select(d => new User
            {
                Name = d.Name,
                Email = d.Email,
                Role = d.Role,
                Team = teams.Single(t => t.Code == d.TeamCode),
                CreatedAt = now.AddDays(-120),
            })
            .ToList();

        var labels = SeedCatalog.LabelDefs
            .Select(d => new Label { Name = d.Name, Color = d.Color })
            .ToList();

        var requesters = users.Where(u => u.Role == UserRole.Client).ToList();
        var samples = TicketSamples.Sample;

        var tickets = samples
            .Select((s, i) =>
            {
                var team = teams.Single(t => t.Code == SeedCatalog.TeamByCategory[s.Category]);
                var createdAt = now.AddDays(-3 * (samples.Length - i)).AddHours(-i);
                var isClosed = s.Status is TicketStatus.Resolved or TicketStatus.Closed;

                return new Ticket
                {
                    Id = Guid.CreateVersion7(createdAt),
                    Title = s.Title,
                    Description = s.Description,
                    Category = s.Category,
                    Status = s.Status,
                    Priority = s.Priority,
                    Requester = requesters[i % requesters.Count],
                    Team = team,
                    // Los tickets abiertos aún no tienen agente asignado.
                    Assignee = s.Status == TicketStatus.Open
                        ? null
                        : users.First(u => u.Role == UserRole.Agent && u.Team == team),
                    CreatedAt = createdAt,
                    ResolvedAt = isClosed ? createdAt.AddDays(1) : null,
                    Labels = labels.Where(l => s.Labels.Contains(l.Name)).ToList(),
                };
            })
            .ToList();

        db.AddRange(departments);
        db.AddRange(teams);
        db.AddRange(users);
        db.AddRange(labels);
        db.AddRange(tickets);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Insertados {Departments} departamentos, {Teams} equipos, {Users} usuarios, {Labels} etiquetas y {Tickets} tickets.",
            departments.Count, teams.Count, users.Count, labels.Count, tickets.Count);
    }

    // Indexa los tickets sin embedding: los recién sembrados y los que la API
    // guardó mientras el proveedor de embeddings no estaba disponible.
    private async Task EmbedMissingAsync(CancellationToken ct)
    {
        var pending = await db.Tickets
            .Where(t => t.Embedding == null)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            logger.LogInformation("Todos los tickets tienen embedding.");
            return;
        }

        logger.LogInformation("Generando embeddings para {Count} tickets…", pending.Count);

        foreach (var batch in pending.Chunk(EmbeddingBatchSize))
        {
            var vectors = await embeddings.EmbedBatchAsync(
                batch.Select(TicketEmbeddingText.For).ToList(), EmbeddingInput.Document, ct);

            for (var i = 0; i < batch.Length; i++)
                batch[i].Embedding = new Vector(vectors[i]);

            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation("Embeddings generados.");
    }
}
