namespace PseudoMarkets.MarketData.Contracts.Quotes;

/// <summary>
/// Detailed quote response for a market symbol.
/// </summary>
public class DetailedQuoteResponse
{
    /// <summary>
    /// Requested market symbol.
    /// </summary>
    /// <example>AAPL</example>
    public required string Symbol { get; init; }

    /// <summary>
    /// Instrument or company name when available from the provider.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Opening price for the current market session.
    /// </summary>
    public decimal Open { get; init; }

    /// <summary>
    /// Highest price for the current market session.
    /// </summary>
    public decimal High { get; init; }

    /// <summary>
    /// Lowest price for the current market session.
    /// </summary>
    public decimal Low { get; init; }

    /// <summary>
    /// Current or latest close price.
    /// </summary>
    public decimal Close { get; init; }

    /// <summary>
    /// Previous close price.
    /// </summary>
    public decimal PreviousClose { get; init; }

    /// <summary>
    /// Absolute price change from previous close.
    /// </summary>
    public decimal Change { get; init; }

    /// <summary>
    /// Percentage price change from previous close.
    /// </summary>
    public decimal ChangePercentage { get; init; }

    /// <summary>
    /// Data source, such as FinnHub or FinnHub Cached.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the response was generated or cached.
    /// </summary>
    public DateTimeOffset TimestampUtc { get; init; }
}
