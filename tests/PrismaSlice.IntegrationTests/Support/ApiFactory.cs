using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PrismaSlice.Application.Abstractions;
using PrismaSlice.Domain.Entities;
using PrismaSlice.Domain.Enums;
using PrismaSlice.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PrismaSlice.IntegrationTests.Support;

/// <summary>
/// Levanta la API real contra un Postgres + pgvector efímero (Testcontainers),
/// con las migraciones aplicadas y un catálogo mínimo de usuarios y etiquetas.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("pgvector/pgvector:pg17").Build();

    public HashingEmbeddingService Embeddings { get; } = new();
    public int RequesterId { get; private set; }

    /// <summary>
    /// Cliente con su propia IP (X-Forwarded-For): cada test tiene su propia cuota
    /// del rate limiter y no depende de cuántas peticiones hicieron los demás.
    /// </summary>
    public HttpClient CreateClientWithOwnIp()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"test-{Guid.NewGuid():N}");
        return client;
    }

    public async ValueTask InitializeAsync()
    {
        await _db.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var department = new Department { Name = "Tecnología", Code = "TECH" };
        var team = new Team { Name = "Soporte TI", Code = "SUP", Department = department };
        var requester = new User
        {
            Name = "Solicitante de prueba",
            Email = "solicitante@test.local",
            Role = UserRole.Client,
            Team = team,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.AddRange(department, team, requester);
        db.Labels.AddRange(new Label { Name = "bug" }, new Label { Name = "pos" });
        await db.SaveChangesAsync();

        RequesterId = requester.Id;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" no carga appsettings.Development.json: la única conexión es la del contenedor.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _db.GetConnectionString());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmbeddingService>();
            services.AddSingleton<IEmbeddingService>(Embeddings);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }
}
