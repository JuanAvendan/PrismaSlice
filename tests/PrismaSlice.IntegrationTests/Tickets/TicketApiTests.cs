using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using PrismaSlice.Application.Tickets;
using PrismaSlice.Domain.Enums;
using PrismaSlice.IntegrationTests.Support;

namespace PrismaSlice.IntegrationTests.Tickets;

public class TicketApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClientWithOwnIp();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private CreateTicketRequest NewTicket(
        string title, string description, bool confidential = false, IReadOnlyList<string>? labels = null) =>
        new(title, description, "Soporte", TicketPriority.Medium, factory.RequesterId,
            IsConfidential: confidential, Labels: labels);

    private async Task<TicketDetails> CreateAsync(CreateTicketRequest request)
    {
        var response = await _client.PostAsJsonAsync("/api/tickets", request, ApiFactory.Json, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TicketDetails>(ApiFactory.Json, Ct))!;
    }

    private async Task<IReadOnlyList<TicketSearchHit>> SearchAsync(string query)
    {
        var response = await _client.GetAsync($"/api/tickets/search?q={Uri.EscapeDataString(query)}", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<TicketSearchHit>>(ApiFactory.Json, Ct))!;
    }

    [Fact]
    public async Task Create_ThenGetByCode_ReturnsTheTicket()
    {
        var response = await _client.PostAsJsonAsync("/api/tickets",
            NewTicket("Solicitud de monitor adicional", "Necesito un segundo monitor para el puesto de facturación.",
                labels: ["pos"]),
            ApiFactory.Json, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<TicketDetails>(ApiFactory.Json, Ct))!;
        Assert.Matches(@"^TCK-\d{5}$", created.Code);
        Assert.Equal($"/api/tickets/{created.Code}", response.Headers.Location?.OriginalString);
        Assert.True(created.IsIndexed);

        var fetched = await _client.GetFromJsonAsync<TicketDetails>(
            $"/api/tickets/{created.Code.ToLowerInvariant()}", ApiFactory.Json, Ct);

        Assert.NotNull(fetched);
        Assert.Equal("Solicitud de monitor adicional", fetched.Title);
        Assert.Equal(TicketStatus.Open, fetched.Status);
        Assert.Equal("Solicitante de prueba", fetched.RequesterName);
        Assert.Equal(["pos"], fetched.Labels);
    }

    [Fact]
    public async Task Create_InvalidRequest_ReturnsValidationProblem()
    {
        var request = NewTicket("", "Sin título", labels: ["no-existe"]) with { RequesterId = -1 };

        var response = await _client.PostAsJsonAsync("/api/tickets", request, ApiFactory.Json, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        Assert.Equal(["labels", "requesterId", "title"], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task Get_UnknownCode_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/tickets/TCK-99999", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Search_RanksTheMostSimilarTicketFirst()
    {
        var printer = await CreateAsync(NewTicket(
            "La impresora de la caja no imprime el recibo",
            "Después del pago la impresora térmica de la caja no imprime el recibo del cliente."));
        await CreateAsync(NewTicket(
            "Restablecer contraseña bloqueada",
            "Un usuario de bodega quedó bloqueado y necesita restablecer su contraseña."));
        await CreateAsync(NewTicket(
            "Catálogo lento en horas pico",
            "La página del catálogo tarda demasiado en cargar las fotos de productos."));

        var hits = await SearchAsync("la impresora no imprime recibos en caja");

        Assert.NotEmpty(hits);
        Assert.Equal(printer.Code, hits[0].Code);
        Assert.True(hits[0].Similarity > hits[^1].Similarity);
        Assert.All(hits, h => Assert.InRange(h.Similarity, -1, 1));
    }

    [Fact]
    public async Task Search_ExcludesConfidentialTickets()
    {
        var secret = await CreateAsync(NewTicket(
            "Auditoría confidencial de nómina",
            "Revisión confidencial de la nómina gerencial.",
            confidential: true));

        var hits = await SearchAsync("auditoría confidencial nómina gerencial");

        Assert.DoesNotContain(hits, h => h.Code == secret.Code);
    }

    [Theory]
    [InlineData("/api/tickets/search")]
    [InlineData("/api/tickets/search?q=%20")]
    [InlineData("/api/tickets/search?q=impresora&limit=0")]
    public async Task Search_InvalidInput_ReturnsBadRequest(string url)
    {
        var response = await _client.GetAsync(url, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EmbeddingProviderDown_CreateStillWorks_SearchReturns503()
    {
        factory.Embeddings.Unavailable = true;
        try
        {
            var created = await CreateAsync(NewTicket(
                "Ticket creado sin IA", "Se guarda aunque el proveedor de embeddings no responda."));
            Assert.False(created.IsIndexed);

            var response = await _client.GetAsync("/api/tickets/search?q=ticket", Ct);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
        finally
        {
            factory.Embeddings.Unavailable = false;
        }
    }
}
