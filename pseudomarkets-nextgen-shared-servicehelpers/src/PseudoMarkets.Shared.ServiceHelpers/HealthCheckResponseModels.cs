namespace PseudoMarkets.Shared.ServiceHelpers;

public sealed record HealthCheckEntryResponse(
    string Status,
    string? Description,
    string Duration,
    IReadOnlyDictionary<string, object?> Data);

public sealed record HealthCheckResponse(
    string Status,
    string TotalDuration,
    IReadOnlyDictionary<string, HealthCheckEntryResponse> Results);
