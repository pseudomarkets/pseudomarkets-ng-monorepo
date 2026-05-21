namespace PseudoMarkets.MarketData.Contracts.Quotes;

/// <summary>
/// Lightweight latest quote response for a market symbol.
/// </summary>
public class QuoteResponse
{
    /// <summary>
    /// Requested market symbol.
    /// </summary>
    /// <example>AAPL</example>
    public required string Symbol { get; init; }

    /// <summary>
    /// Latest available quote price.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Data source, such as FinnHub or FinnHub Cached.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the response was generated or cached.
    /// </summary>
    public DateTimeOffset TimestampUtc { get; init; }
}
