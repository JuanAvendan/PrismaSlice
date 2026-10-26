using Microsoft.Extensions.Logging;
using Pgvector;
using PrismaSlice.Application.Abstractions;
using PrismaSlice.Application.Common;
using PrismaSlice.Domain.Entities;
using PrismaSlice.Domain.Enums;

namespace PrismaSlice.Application.Tickets;

public sealed class TicketService(
    ITicketRepository tickets,
    IEmbeddingService embeddings,
    TimeProvider time,
    ILogger<TicketService> logger)
{
    public const int TitleMaxLength = 200;
    public const int CategoryMaxLength = 80;
    public const int QueryMaxLength = 500;
    public const int DefaultSearchLimit = 10;
    public const int MaxSearchLimit = 50;

    public async Task<TicketDetails> CreateAsync(
        CreateTicketRequest request, CancellationToken ct = default)
    {
        var errors = new ValidationErrors();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors.Add("title", "El título es obligatorio.");
        else if (request.Title.Trim().Length > TitleMaxLength)
            errors.Add("title", $"El título admite máximo {TitleMaxLength} caracteres.");

        if (string.IsNullOrWhiteSpace(request.Description))
            errors.Add("description", "La descripción es obligatoria.");

        if (string.IsNullOrWhiteSpace(request.Category))
            errors.Add("category", "La categoría es obligatoria.");
        else if (request.Category.Trim().Length > CategoryMaxLength)
            errors.Add("category", $"La categoría admite máximo {CategoryMaxLength} caracteres.");

        if (!Enum.IsDefined(request.Priority))
            errors.Add("priority", "Prioridad no válida.");

        if (!await tickets.UserExistsAsync(request.RequesterId, ct))
            errors.Add("requesterId", $"No existe el usuario {request.RequesterId}.");

        if (request.TeamId is { } teamId && !await tickets.TeamExistsAsync(teamId, ct))
            errors.Add("teamId", $"No existe el equipo {teamId}.");

        var labelNames = (request.Labels ?? [])
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();
        var labels = await tickets.GetLabelsAsync(labelNames, ct);
        foreach (var unknown in labelNames.Except(labels.Select(l => l.Name)))
            errors.Add("labels", $"No existe la etiqueta '{unknown}'.");

        errors.ThrowIfAny();

        var ticket = new Ticket
        {
            Id = Guid.CreateVersion7(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            Priority = request.Priority,
            Status = TicketStatus.Open,
            IsConfidential = request.IsConfidential,
            RequesterId = request.RequesterId,
            TeamId = request.TeamId,
            CreatedAt = time.GetUtcNow(),
            Labels = labels.ToList(),
        };
        ticket.Embedding = await TryEmbedAsync(ticket, ct);

        await tickets.AddAsync(ticket, ct);

        return await tickets.GetDetailsAsync(ticket.Code, ct)
            ?? throw new InvalidOperationException($"El ticket {ticket.Code} no se pudo leer tras crearlo.");
    }

    public Task<TicketDetails?> GetAsync(string code, CancellationToken ct = default) =>
        tickets.GetDetailsAsync(code.Trim().ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<TicketSearchHit>> SearchAsync(
        string? query, int? limit = null, CancellationToken ct = default)
    {
        var errors = new ValidationErrors();
        var take = limit ?? DefaultSearchLimit;

        if (string.IsNullOrWhiteSpace(query))
            errors.Add("q", "La consulta es obligatoria.");
        else if (query.Length > QueryMaxLength)
            errors.Add("q", $"La consulta admite máximo {QueryMaxLength} caracteres.");

        if (take is < 1 or > MaxSearchLimit)
            errors.Add("limit", $"El límite debe estar entre 1 y {MaxSearchLimit}.");

        errors.ThrowIfAny();

        float[] queryEmbedding;
        try
        {
            queryEmbedding = await embeddings.EmbedAsync(query!.Trim(), EmbeddingInput.Query, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            throw new EmbeddingUnavailableException(ex);
        }

        return await tickets.SearchAsync(queryEmbedding, take, ct);
    }

    // Crear un ticket no debe fallar porque la IA esté caída: se guarda sin
    // embedding y el backfill (tools/PrismaSlice.SeedData) lo completa después.
    private async Task<Vector?> TryEmbedAsync(Ticket ticket, CancellationToken ct)
    {
        try
        {
            var embedding = await embeddings.EmbedAsync(
                TicketEmbeddingText.For(ticket), EmbeddingInput.Document, ct);
            return new Vector(embedding);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex,
                "No se pudo generar el embedding del ticket {TicketId}; se guarda sin indexar.",
                ticket.Id);
            return null;
        }
    }
}
