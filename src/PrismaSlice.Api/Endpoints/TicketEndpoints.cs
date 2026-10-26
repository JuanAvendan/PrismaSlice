using Microsoft.AspNetCore.Http.HttpResults;
using PrismaSlice.Api.Hosting;
using PrismaSlice.Application.Tickets;

namespace PrismaSlice.Api.Endpoints;

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tickets").WithTags("Tickets");

        group.MapPost("/", CreateAsync)
            .WithName("CreateTicket")
            .WithSummary("Crea un ticket y genera su embedding.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimiting.WritePolicy);

        group.MapGet("/search", SearchAsync)
            .WithName("SearchTickets")
            .WithSummary("Búsqueda semántica: tickets parecidos a la consulta, por similitud coseno.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimiting.SearchPolicy);

        group.MapGet("/{code}", GetAsync)
            .WithName("GetTicket")
            .WithSummary("Detalle de un ticket por su código (p. ej. TCK-00042).")
            .RequireRateLimiting(RateLimiting.SearchPolicy);

        return app;
    }

    private static async Task<Created<TicketDetails>> CreateAsync(
        CreateTicketRequest request, TicketService tickets, CancellationToken ct)
    {
        var ticket = await tickets.CreateAsync(request, ct);
        return TypedResults.Created($"/api/tickets/{ticket.Code}", ticket);
    }

    private static async Task<Results<Ok<TicketDetails>, NotFound>> GetAsync(
        string code, TicketService tickets, CancellationToken ct) =>
        await tickets.GetAsync(code, ct) is { } ticket
            ? TypedResults.Ok(ticket)
            : TypedResults.NotFound();

    private static async Task<Ok<IReadOnlyList<TicketSearchHit>>> SearchAsync(
        string? q, int? limit, TicketService tickets, CancellationToken ct) =>
        TypedResults.Ok(await tickets.SearchAsync(q, limit, ct));
}
