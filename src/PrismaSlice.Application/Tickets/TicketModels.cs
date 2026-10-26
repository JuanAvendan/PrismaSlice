using PrismaSlice.Domain.Enums;

namespace PrismaSlice.Application.Tickets;

public sealed record CreateTicketRequest(
    string Title,
    string Description,
    string Category,
    TicketPriority Priority,
    int RequesterId,
    int? TeamId = null,
    bool IsConfidential = false,
    IReadOnlyList<string>? Labels = null);

public sealed record TicketDetails(
    string Code,
    string Title,
    string Description,
    string Category,
    TicketStatus Status,
    TicketPriority Priority,
    bool IsConfidential,
    string RequesterName,
    string? AssigneeName,
    string? TeamName,
    IReadOnlyList<string> Labels,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    bool IsIndexed);

public sealed record TicketSearchHit(
    string Code,
    string Title,
    string Category,
    TicketStatus Status,
    TicketPriority Priority,
    double Similarity);
