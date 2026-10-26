using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PrismaSlice.Application.Abstractions;
using PrismaSlice.Infrastructure.Embeddings;

namespace PrismaSlice.IntegrationTests.Embeddings;

/// <summary>
/// Corre EmbeddingGemma real (ONNX). Si el modelo no está descargado el test se
/// salta, salvo que REQUIRE_EMBEDDING_MODEL=true (así en CI falla en vez de saltarse).
/// </summary>
public sealed class OnnxModelFixture : IDisposable
{
    public string? ModelDirectory { get; } = FindModelDirectory();

    public OnnxEmbeddingService Create(int maxTokens = 512) => new(
        Options.Create(new EmbeddingOptions
        {
            Onnx = { ModelDirectory = ModelDirectory!, MaxTokens = maxTokens },
        }),
        NullLogger<OnnxEmbeddingService>.Instance);

    public OnnxEmbeddingService Service => _service ??= Create();
    private OnnxEmbeddingService? _service;

    public void RequireModel()
    {
        if (ModelDirectory is not null)
            return;

        const string hint = "Modelo ONNX no encontrado; ejecuta scripts/download-model.ps1 (o .sh).";
        if (Environment.GetEnvironmentVariable("REQUIRE_EMBEDDING_MODEL") == "true")
            Assert.Fail(hint);
        Assert.Skip(hint);
    }

    public void Dispose() => _service?.Dispose();

    // Sube desde bin/ hasta encontrar models/embeddinggemma-300m en la raíz del repo.
    private static string? FindModelDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "models", "embeddinggemma-300m");
            if (File.Exists(Path.Combine(candidate, "model_quantized.onnx")))
                return candidate;
        }
        return null;
    }
}

public class OnnxEmbeddingServiceTests(OnnxModelFixture model) : IClassFixture<OnnxModelFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const string PrinterTicket =
        "La caja 3 no imprime el recibo tras el pago\nPunto de venta (POS)\n" +
        "La impresora térmica procesa el cobro pero no emite el recibo.";
    private const string PasswordTicket =
        "Restablecer contraseña de usuario bloqueado\nCuentas y accesos\n" +
        "Un usuario de bodega quedó bloqueado tras varios intentos fallidos de inicio de sesión.";
    private const string BarcodeTicket =
        "El lector de código de barras no reconoce productos nuevos\nPunto de venta (POS)\n" +
        "Los productos de la última colección no son reconocidos por el lector en caja.";

    [Fact]
    public async Task Embeddings_Have768NormalizedDimensions()
    {
        model.RequireModel();

        var embedding = await model.Service.EmbedAsync("hola", EmbeddingInput.Query, Ct);

        Assert.Equal(768, embedding.Length);
        Assert.Equal(1.0, Math.Sqrt(embedding.Sum(v => (double)v * v)), precision: 3);
    }

    [Fact]
    public async Task Batch_MatchesOneByOne_DespiteDifferentLengths()
    {
        model.RequireModel();
        string[] texts = ["corto", PrinterTicket];

        var batch = await model.Service.EmbedBatchAsync(texts, EmbeddingInput.Document, Ct);

        for (var i = 0; i < texts.Length; i++)
        {
            var single = await model.Service.EmbedAsync(texts[i], EmbeddingInput.Document, Ct);
            Assert.True(Cosine(batch[i], single) > 0.999, $"El relleno alteró el embedding {i}.");
        }
    }

    [Theory]
    [InlineData("el sistema de cobro no saca el comprobante", 0)]
    [InlineData("olvidé mi clave y no puedo entrar", 1)]
    [InlineData("barcode scanner does not read new products", 2)]
    public async Task Query_IsClosestToTheTicketWithTheSameMeaning(string query, int expected)
    {
        model.RequireModel();
        var documents = await model.Service.EmbedBatchAsync(
            [PrinterTicket, PasswordTicket, BarcodeTicket], EmbeddingInput.Document, Ct);

        var q = await model.Service.EmbedAsync(query, EmbeddingInput.Query, Ct);
        var best = Enumerable.Range(0, documents.Count).MaxBy(i => Cosine(q, documents[i]));

        Assert.Equal(expected, best);
    }

    [Fact]
    public async Task LongText_IsTruncatedToMaxTokens()
    {
        model.RequireModel();
        using var service = model.Create(maxTokens: 16);

        var embedding = await service.EmbedAsync(
            string.Join(' ', Enumerable.Repeat(PrinterTicket, 20)), EmbeddingInput.Document, Ct);

        Assert.Equal(768, embedding.Length);
    }

    private static double Cosine(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        return dot / Math.Sqrt(na * nb);
    }
}
