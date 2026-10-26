using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrismaSlice.Application.Abstractions;

namespace PrismaSlice.Infrastructure.Embeddings;

public static class EmbeddingServiceExtensions
{
    public static IServiceCollection AddEmbeddings(
        this IServiceCollection services, IConfiguration config)
    {
        services.Configure<EmbeddingOptions>(
            config.GetSection(EmbeddingOptions.SectionName));

        var opt = config.GetSection(EmbeddingOptions.SectionName)
            .Get<EmbeddingOptions>() ?? new EmbeddingOptions();

        switch (opt.Provider.ToLowerInvariant())
        {
            case "onnx":
                // Singleton: la sesión de ONNX Runtime es costosa de crear y segura entre hilos.
                services.AddSingleton<IEmbeddingService, OnnxEmbeddingService>();
                break;

            case "openai":
                services.AddHttpClient<IEmbeddingService, OpenAIEmbeddingService>(c =>
                {
                    c.BaseAddress = new Uri("https://api.openai.com");
                    c.DefaultRequestHeaders.Authorization =
                        new("Bearer", opt.OpenAI.ApiKey);
                });
                break;

            case "ollama":
                services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(c =>
                    c.BaseAddress = new Uri(opt.Ollama.BaseUrl));
                break;

            default:
                throw new InvalidOperationException(
                    $"Embeddings:Provider '{opt.Provider}' no es válido. Usa Onnx, Ollama u OpenAI.");
        }

        return services;
    }
}