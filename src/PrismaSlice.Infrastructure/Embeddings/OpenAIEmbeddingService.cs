using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PrismaSlice.Application.Abstractions;

namespace PrismaSlice.Infrastructure.Embeddings;

public class OpenAIEmbeddingService(
    HttpClient http, IOptions<EmbeddingOptions> options) : IEmbeddingService
{
    private readonly EmbeddingOptions _opt = options.Value;

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var results = await EmbedBatchAsync([text], ct);
        return results[0];
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var req = new
        {
            model = _opt.OpenAI.Model,
            input = texts,
            dimensions = _opt.Dimensions   // trunca a 768
        };

        var res = await http.PostAsJsonAsync("/v1/embeddings", req, ct);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<OpenAIResponse>(ct)
            ?? throw new InvalidOperationException("Respuesta vacía de OpenAI.");

        return body.Data
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding)
            .ToList();
    }

    private sealed record OpenAIResponse(List<EmbeddingData> Data);
    private sealed record EmbeddingData(int Index, float[] Embedding);
}