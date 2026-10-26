using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PrismaSlice.Application.Abstractions;

namespace PrismaSlice.Infrastructure.Embeddings;

public class OllamaEmbeddingService(
    HttpClient http, IOptions<EmbeddingOptions> options) : IEmbeddingService
{
    private readonly EmbeddingOptions _opt = options.Value;

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var req = new { model = _opt.Ollama.Model, prompt = text };
        var res = await http.PostAsJsonAsync("/api/embeddings", req, ct);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<OllamaResponse>(ct)
            ?? throw new InvalidOperationException("Respuesta vacía de Ollama.");

        if (body.Embedding.Length != _opt.Dimensions)
            throw new InvalidOperationException(
                $"Esperaba {_opt.Dimensions} dims, llegaron {body.Embedding.Length}.");

        return body.Embedding;
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        // Ollama no tiene batch nativo en /api/embeddings; secuencial.
        var results = new List<float[]>(texts.Count);
        foreach (var t in texts)
            results.Add(await EmbedAsync(t, ct));
        return results;
    }

    private sealed record OllamaResponse(float[] Embedding);
}