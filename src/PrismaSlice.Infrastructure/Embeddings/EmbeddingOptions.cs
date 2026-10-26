namespace PrismaSlice.Infrastructure.Embeddings;

public class EmbeddingOptions
{
    public const string SectionName = "Embeddings";

    /// <summary>"Ollama" u "OpenAI".</summary>
    public string Provider { get; set; } = "Ollama";

    /// <summary>Debe coincidir con la columna vector(N) de la base.</summary>
    public int Dimensions { get; set; } = 768;

    public OllamaOptions Ollama { get; set; } = new();
    public OpenAIOptions OpenAI { get; set; } = new();

    public class OllamaOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        // EmbeddingGemma: multilingüe (los tickets están en español) y 768 dimensiones.
        public string Model { get; set; } = "embeddinggemma";

        // Prompts de tarea recomendados para EmbeddingGemma en recuperación.
        // Otros modelos usan otros (p. ej. nomic-embed-text: "search_document: " / "search_query: ").
        public string DocumentPrefix { get; set; } = "title: none | text: ";
        public string QueryPrefix { get; set; } = "task: search result | query: ";
    }

    public class OpenAIOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "text-embedding-3-small";
    }
}
