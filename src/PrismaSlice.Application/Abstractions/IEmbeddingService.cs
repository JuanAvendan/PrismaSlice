namespace PrismaSlice.Application.Abstractions;

public interface IEmbeddingService
{
    /// <summary>Genera el embedding de un texto. Devuelve un float[768].</summary>
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);

    /// <summary>Batch: varios textos de una. Implementaciones pueden optimizar.</summary>
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default);
}