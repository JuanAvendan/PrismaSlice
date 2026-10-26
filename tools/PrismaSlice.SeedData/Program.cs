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
catch (HttpRequestException ex)
{
    host.Services.GetRequiredService<ILogger<Seeder>>().LogError(ex,
        "No se pudo contactar el proveedor de embeddings. Si usas Ollama: " +
        "instálalo, ejecuta 'ollama pull embeddinggemma' y vuelve a correr el seed. " +
        "Los datos ya insertados se conservan.");
    return 1;
}

return 0;
