using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PrismaSlice.Application.Abstractions;

namespace PrismaSlice.Infrastructure.Embeddings;

public class OllamaEmbeddingService(
    HttpClient http, IOptions<EmbeddingOptions> options) : IEmbeddingService
{
    private readonly EmbeddingOptions _opt = options.Value;

    public async Task<float[]> EmbedAsync(
        string text, EmbeddingInput input, CancellationToken ct = default)
    {
        var results = await EmbedBatchAsync([text], input, ct);
        return results[0];
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts, EmbeddingInput input, CancellationToken ct = default)
    {
        var prefix = input == EmbeddingInput.Query
            ? _opt.Ollama.QueryPrefix
            : _opt.Ollama.DocumentPrefix;

        // /api/embed acepta varios textos en una sola llamada.
        var req = new { model = _opt.Ollama.Model, input = texts.Select(t => prefix + t) };
        var res = await http.PostAsJsonAsync("/api/embed", req, ct);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<OllamaResponse>(ct)
            ?? throw new InvalidOperationException("Respuesta vacía de Ollama.");

        foreach (var embedding in body.Embeddings)
            EmbeddingGuard.EnsureDimensions(embedding, _opt.Dimensions);

        return body.Embeddings;
    }

    private sealed record OllamaResponse(float[][] Embeddings);
}
