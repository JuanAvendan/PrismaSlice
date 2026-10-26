namespace PrismaSlice.Infrastructure.Embeddings;

public class EmbeddingOptions
{
    public const string SectionName = "Embeddings";

    /// <summary>"Ollama" u "OpenAI".</summary>
    public string Provider { get; set; } = "Ollama";
    public int Dimensions { get; set; } = 768;

    public OllamaOptions Ollama { get; set; } = new();
    public OpenAIOptions OpenAI { get; set; } = new();

    public class OllamaOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "nomic-embed-text";
    }

    public class OpenAIOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "text-embedding-3-small";
    }
}