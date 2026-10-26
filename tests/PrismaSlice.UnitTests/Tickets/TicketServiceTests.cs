using Microsoft.Extensions.Logging.Abstractions;
using PrismaSlice.Application.Abstractions;
using PrismaSlice.Application.Common;
using PrismaSlice.Application.Tickets;
using PrismaSlice.Domain.Enums;
using PrismaSlice.UnitTests.Fakes;

namespace PrismaSlice.UnitTests.Tickets;

public class TicketServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeTicketRepository _repository = new();
    private readonly FakeEmbeddingService _embeddings = new();
    private readonly TicketService _service;

    public TicketServiceTests()
    {
        _service = new TicketService(
            _repository, _embeddings, new FixedTimeProvider(Now), NullLogger<TicketService>.Instance);
    }

    private static CreateTicketRequest ValidRequest(IReadOnlyList<string>? labels = null) => new(
        Title: "  La caja 3 no imprime el recibo  ",
        Description: "La impresora térmica no emite el recibo tras el pago.",
        Category: "Punto de venta (POS)",
        Priority: TicketPriority.High,
        RequesterId: 1,
        TeamId: 1,
        Labels: labels ?? ["BUG"]);

    [Fact]
    public async Task CreateAsync_ValidRequest_SavesOpenIndexedTicket()
    {
        var ticket = await _service.CreateAsync(ValidRequest(), TestContext.Current.CancellationToken);

        Assert.Equal("TCK-00001", ticket.Code);
        Assert.Equal("La caja 3 no imprime el recibo", ticket.Title);
        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Equal(Now, ticket.CreatedAt);
        Assert.Equal(["bug"], ticket.Labels);
        Assert.True(ticket.IsIndexed);

        var (text, input) = Assert.Single(_embeddings.Calls);
        Assert.Equal(EmbeddingInput.Document, input);
        Assert.Contains("La caja 3 no imprime el recibo", text);
        Assert.Contains("La impresora térmica", text);
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ReportsEveryError()
    {
        var request = ValidRequest(labels: ["no-existe"]) with
        {
            Title = " ",
            Description = "",
            RequesterId = 99,
            TeamId = 42,
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(request, TestContext.Current.CancellationToken));

        Assert.Equal(
            ["description", "labels", "requesterId", "teamId", "title"],
            ex.Errors.Keys.Order());
        Assert.Empty(_repository.Saved);
    }

    [Fact]
    public async Task CreateAsync_TitleTooLong_IsRejected()
    {
        var request = ValidRequest() with { Title = new string('x', TicketService.TitleMaxLength + 1) };

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(request, TestContext.Current.CancellationToken));

        Assert.Equal(["title"], ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_EmbeddingProviderDown_StillSavesTicketWithoutEmbedding()
    {
        _embeddings.Unavailable = true;

        var ticket = await _service.CreateAsync(ValidRequest(), TestContext.Current.CancellationToken);

        Assert.False(ticket.IsIndexed);
        Assert.Null(Assert.Single(_repository.Saved).Embedding);
    }

    [Fact]
    public async Task SearchAsync_EmbedsQueryAsQueryAndUsesDefaultLimit()
    {
        await _service.SearchAsync("  impresora sin recibo ", ct: TestContext.Current.CancellationToken);

        Assert.Equal(("impresora sin recibo", EmbeddingInput.Query), Assert.Single(_embeddings.Calls));
        Assert.Equal(TicketService.DefaultSearchLimit, _repository.LastSearch?.Limit);
    }

    [Theory]
    [InlineData(null, 10, "q")]
    [InlineData("   ", 10, "q")]
    [InlineData("impresora", 0, "limit")]
    [InlineData("impresora", TicketService.MaxSearchLimit + 1, "limit")]
    public async Task SearchAsync_InvalidInput_IsRejected(string? query, int limit, string field)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.SearchAsync(query, limit, TestContext.Current.CancellationToken));

        Assert.Equal([field], ex.Errors.Keys);
        Assert.Empty(_embeddings.Calls);
    }

    [Fact]
    public async Task SearchAsync_EmbeddingProviderDown_ThrowsEmbeddingUnavailable()
    {
        _embeddings.Unavailable = true;

        await Assert.ThrowsAsync<EmbeddingUnavailableException>(
            () => _service.SearchAsync("impresora", ct: TestContext.Current.CancellationToken));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
