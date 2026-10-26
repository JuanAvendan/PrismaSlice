using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using PrismaSlice.Application.Abstractions;

namespace PrismaSlice.Infrastructure.Embeddings;

/// <summary>
/// EmbeddingGemma corriendo dentro del proceso con ONNX Runtime: sin servidor de
/// modelos ni API externa. El modelo se carga la primera vez que se usa, así un
/// archivo faltante solo afecta a los embeddings y no al resto de la API.
/// </summary>
public sealed class OnnxEmbeddingService(
    IOptions<EmbeddingOptions> options,
    ILogger<OnnxEmbeddingService> logger) : IEmbeddingService, IDisposable
{
    private readonly EmbeddingOptions _opt = options.Value;
    private readonly Lock _loadLock = new();
    private LoadedModel? _model;

    public async Task<float[]> EmbedAsync(
        string text, EmbeddingInput input, CancellationToken ct = default)
    {
        var results = await EmbedBatchAsync([text], input, ct);
        return results[0];
    }

    public Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts, EmbeddingInput input, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (texts.Count == 0)
            return Task.FromResult<IReadOnlyList<float[]>>([]);

        var model = GetModel();
        var prefix = _opt.PrefixFor(input);
        var tokenized = texts.Select(t => Tokenize(model.Tokenizer, prefix + t)).ToList();

        // Un solo tensor [batch, longitud máxima]. DenseTensor arranca en ceros, que es
        // a la vez el id de <pad> en Gemma y attention_mask = 0 para el relleno.
        var length = tokenized.Max(ids => ids.Count);
        var inputIds = new DenseTensor<long>([texts.Count, length]);
        var attentionMask = new DenseTensor<long>([texts.Count, length]);
        for (var i = 0; i < tokenized.Count; i++)
        {
            for (var j = 0; j < tokenized[i].Count; j++)
            {
                inputIds[i, j] = tokenized[i][j];
                attentionMask[i, j] = 1;
            }
        }

        // La salida sentence_embedding ya trae pooling, capas densas y normalización.
        using var results = model.Session.Run(
        [
            NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
        ], ["sentence_embedding"]);

        var output = results[0].AsTensor<float>();
        var dimensions = output.Dimensions[1];
        var embeddings = new List<float[]>(texts.Count);
        for (var i = 0; i < texts.Count; i++)
        {
            var embedding = new float[dimensions];
            for (var j = 0; j < dimensions; j++)
                embedding[j] = output[i, j];

            EmbeddingGuard.EnsureDimensions(embedding, _opt.Dimensions);
            embeddings.Add(embedding);
        }

        return Task.FromResult<IReadOnlyList<float[]>>(embeddings);
    }

    public void Dispose() => _model?.Session.Dispose();

    private IReadOnlyList<int> Tokenize(SentencePieceTokenizer tokenizer, string text)
    {
        var ids = tokenizer.EncodeToIds(text);
        if (ids.Count <= _opt.Onnx.MaxTokens)
            return ids;

        // Se recorta conservando <eos> al final, igual que el tokenizador de Hugging Face.
        return [.. ids.Take(_opt.Onnx.MaxTokens - 1), tokenizer.EndOfSentenceId];
    }

    private LoadedModel GetModel()
    {
        if (Volatile.Read(ref _model) is { } loaded)
            return loaded;

        // Sin Lazy<T>: si falla (p. ej. falta el archivo) se reintenta en la próxima llamada
        // en vez de cachear la excepción hasta reiniciar.
        lock (_loadLock)
            return _model ??= Load();
    }

    // Una ruta relativa se busca desde el directorio de trabajo hacia arriba: así sirve
    // igual con `dotnet run` (cwd = carpeta del proyecto), desde la raíz del repo y en Docker (/app).
    private string ResolveModelDirectory()
    {
        var configured = _opt.Onnx.ModelDirectory;
        if (Path.IsPathRooted(configured))
            return configured;

        for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, configured);
            if (Directory.Exists(candidate))
                return candidate;
        }

        return Path.GetFullPath(configured);
    }

    private LoadedModel Load()
    {
        var directory = ResolveModelDirectory();
        var modelPath = Path.Combine(directory, _opt.Onnx.ModelFile);
        var tokenizerPath = Path.Combine(directory, _opt.Onnx.TokenizerFile);

        if (!File.Exists(modelPath) || !File.Exists(tokenizerPath))
            throw new FileNotFoundException(
                $"No se encontró el modelo de embeddings en '{directory}'. " +
                "Descárgalo con scripts/download-model.ps1 (Windows) o scripts/download-model.sh.",
                modelPath);

        var stopwatch = Stopwatch.StartNew();

        SentencePieceTokenizer tokenizer;
        using (var stream = File.OpenRead(tokenizerPath))
            tokenizer = SentencePieceTokenizer.Create(
                stream, addBeginningOfSentence: true, addEndOfSentence: true);

        using var sessionOptions = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
        };
        var session = new InferenceSession(modelPath, sessionOptions);

        logger.LogInformation(
            "Modelo de embeddings cargado desde {ModelPath} en {ElapsedMs} ms.",
            modelPath, stopwatch.ElapsedMilliseconds);

        return new LoadedModel(session, tokenizer);
    }

    private sealed record LoadedModel(InferenceSession Session, SentencePieceTokenizer Tokenizer);
}
