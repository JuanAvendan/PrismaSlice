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
        public string Model { get; set; } = "nomic-embed-text";

        // nomic-embed-text está entrenado con estos prefijos de tarea.
        public string DocumentPrefix { get; set; } = "search_document: ";
        public string QueryPrefix { get; set; } = "search_query: ";
    }

    public class OpenAIOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "text-embedding-3-small";
    }
}
