using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PrismaSlice.Application.Abstractions;

namespace PrismaSlice.IntegrationTests.Support;

/// <summary>
/// Embedding determinista para tests: bolsa de palabras proyectada con hashing
/// sobre 768 dimensiones y normalizada. Textos con palabras en común quedan
/// cerca en distancia coseno, así que la búsqueda se puede probar de punta a
/// punta (API → pgvector → HNSW) sin depender de un modelo real.
/// </summary>
public sealed partial class HashingEmbeddingService : IEmbeddingService
{
    private const int Dimensions = 768;

    /// <summary>Simula que el proveedor de embeddings está caído.</summary>
    public bool Unavailable { get; set; }

    public Task<float[]> EmbedAsync(
        string text, EmbeddingInput input, CancellationToken ct = default)
    {
        if (Unavailable)
            throw new HttpRequestException("Proveedor de embeddings simulado caído.");

        var vector = new float[Dimensions];
        foreach (var token in Tokenize(text))
            vector[Bucket(token)] += 1;

        var norm = MathF.Sqrt(vector.Sum(v => v * v));
        if (norm == 0)
        {
            vector[0] = 1; // pgvector no define el coseno de un vector nulo
            return Task.FromResult(vector);
        }

        for (var i = 0; i < vector.Length; i++)
            vector[i] /= norm;

        return Task.FromResult(vector);
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts, EmbeddingInput input, CancellationToken ct = default)
    {
        var results = new List<float[]>(texts.Count);
        foreach (var text in texts)
            results.Add(await EmbedAsync(text, input, ct));
        return results;
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        var withoutAccents = new string(text
            .Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());

        return NonWordChars()
            .Split(withoutAccents.ToLowerInvariant())
            .Where(t => t.Length >= 3);
    }

    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex NonWordChars();

    // FNV-1a: estable entre ejecuciones, a diferencia de string.GetHashCode().
    private static int Bucket(string token)
    {
        var hash = 2166136261u;
        foreach (var c in token)
            hash = (hash ^ c) * 16777619u;
        return (int)(hash % Dimensions);
    }
}

