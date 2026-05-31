namespace PseudoMarkets.BalancesAndPositions.Core.Models;

public sealed record QuoteLookupResult(
    bool IsQuoteAvailable,
    decimal? Price,
    string? WarningCode,
    string? WarningMessage);
