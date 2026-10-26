namespace PrismaSlice.Application.Abstractions;

/// <summary>
/// Qué se está embebiendo. Algunos modelos (p. ej. nomic-embed-text) esperan
/// prefijos distintos para documentos indexados y para consultas de búsqueda.
/// </summary>
public enum EmbeddingInput { Document, Query }

public interface IEmbeddingService
{
    /// <summary>Genera el embedding de un texto.</summary>
    Task<float[]> EmbedAsync(
        string text, EmbeddingInput input, CancellationToken ct = default);

    /// <summary>Genera embeddings para varios textos, en el mismo orden.</summary>
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts, EmbeddingInput input, CancellationToken ct = default);
}
