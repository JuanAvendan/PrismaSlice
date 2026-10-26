using PrismaSlice.Application.Abstractions;

namespace PrismaSlice.Infrastructure.Embeddings;

public class EmbeddingOptions
{
    public const string SectionName = "Embeddings";

    /// <summary>"Onnx" (modelo dentro del proceso), "Ollama" u "OpenAI".</summary>
    public string Provider { get; set; } = "Onnx";

    /// <summary>Debe coincidir con la columna vector(N) de la base.</summary>
    public int Dimensions { get; set; } = 768;

    // Prompts de tarea recomendados para EmbeddingGemma en recuperación. Aplican a
    // Onnx y Ollama, que corren ese modelo; OpenAI no los usa.
    public string DocumentPrefix { get; set; } = "title: none | text: ";
    public string QueryPrefix { get; set; } = "task: search result | query: ";

    public OnnxOptions Onnx { get; set; } = new();
    public OllamaOptions Ollama { get; set; } = new();
    public OpenAIOptions OpenAI { get; set; } = new();

    public string PrefixFor(EmbeddingInput input) =>
        input == EmbeddingInput.Query ? QueryPrefix : DocumentPrefix;

    public class OnnxOptions
    {
        /// <summary>
        /// Carpeta con el modelo y el tokenizador. Si es relativa, se resuelve
        /// contra el directorio de trabajo (la raíz del repo en local, /app en Docker).
        /// </summary>
        public string ModelDirectory { get; set; } = "models/embeddinggemma-300m";
        public string ModelFile { get; set; } = "model_quantized.onnx";
        public string TokenizerFile { get; set; } = "tokenizer.model";

        /// <summary>Tope de tokens por texto; acota latencia y memoria (el modelo admite 2048).</summary>
        public int MaxTokens { get; set; } = 512;
    }

    public class OllamaOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "embeddinggemma";
    }

    public class OpenAIOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "text-embedding-3-small";
    }
}
