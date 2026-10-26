using PrismaSlice.Application.Abstractions;

namespace PrismaSlice.UnitTests.Fakes;

internal sealed class FakeEmbeddingService : IEmbeddingService
{
    public bool Unavailable { get; set; }
    public List<(string Text, EmbeddingInput Input)> Calls { get; } = [];

    public Task<float[]> EmbedAsync(
        string text, EmbeddingInput input, CancellationToken ct = default)
    {
        if (Unavailable)
            throw new HttpRequestException("Proveedor simulado caído.");

        Calls.Add((text, input));
        var embedding = new float[768];
        embedding[0] = 1;
        return Task.FromResult(embedding);
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts, EmbeddingInput input, CancellationToken ct = default)
    {
        var results = new List<float[]>();
        foreach (var text in texts)
            results.Add(await EmbedAsync(text, input, ct));
        return results;
    }
}
