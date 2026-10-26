namespace PrismaSlice.Application.Common;

/// <summary>El proveedor de embeddings no respondió. La API la traduce a 503.</summary>
public sealed class EmbeddingUnavailableException(Exception inner)
    : Exception("El servicio de embeddings no está disponible.", inner);
