using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrismaSlice.Infrastructure;
using PrismaSlice.SeedData;

// Uso: dotnet run --project tools/PrismaSlice.SeedData
// Aplica migraciones, siembra datos sintéticos si la base está vacía y genera
// los embeddings que falten. Es idempotente: se puede correr varias veces.
// La conexión y el proveedor se pueden sobrescribir con variables de entorno,
// p. ej. ConnectionStrings__Default o Embeddings__Provider.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<Seeder>();

using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();

try
{
    await scope.ServiceProvider.GetRequiredService<Seeder>().RunAsync();
}
catch (Exception ex) when (ex is FileNotFoundException or HttpRequestException)
{
    host.Services.GetRequiredService<ILogger<Seeder>>().LogError(ex,
        "No se pudieron generar los embeddings. Con el proveedor Onnx, descarga el modelo " +
        "con scripts/download-model.ps1 (o .sh); con Ollama, ejecuta 'ollama pull embeddinggemma'. " +
        "Los datos ya insertados se conservan: vuelve a correr el seed.");
    return 1;
}

return 0;
