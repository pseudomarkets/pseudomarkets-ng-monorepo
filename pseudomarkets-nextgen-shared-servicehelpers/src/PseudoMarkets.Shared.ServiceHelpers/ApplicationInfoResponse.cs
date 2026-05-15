namespace PseudoMarkets.Shared.ServiceHelpers;

public sealed record ApplicationInfoResponse(
    string Name,
    string Version,
    string BuildTimestamp);
