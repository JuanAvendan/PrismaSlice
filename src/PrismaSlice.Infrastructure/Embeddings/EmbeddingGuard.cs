namespace PrismaSlice.Infrastructure.Embeddings;

internal static class EmbeddingGuard
{
    public static void EnsureDimensions(float[] embedding, int expected)
    {
        if (embedding.Length != expected)
            throw new InvalidOperationException(
                $"Esperaba {expected} dimensiones, llegaron {embedding.Length}. " +
                "Revisa Embeddings:Dimensions y el modelo configurado.");
    }
}
