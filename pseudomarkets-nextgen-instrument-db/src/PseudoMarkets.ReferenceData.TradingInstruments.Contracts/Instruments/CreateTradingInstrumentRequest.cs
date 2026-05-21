namespace PseudoMarkets.ReferenceData.TradingInstruments.Contracts.Instruments;

/// <summary>
/// Request used to create a trading instrument reference-data record.
/// </summary>
public sealed record CreateTradingInstrumentRequest
{
    /// <summary>
    /// Unique market symbol for the instrument.
    /// </summary>
    /// <example>AAPL</example>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable instrument description.
    /// </summary>
    /// <example>Apple Inc.</example>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Primary instrument type.
    /// </summary>
    /// <example>EQUITY</example>
    public string PrimaryInstrumentType { get; init; } = string.Empty;

    /// <summary>
    /// Secondary instrument type.
    /// </summary>
    /// <example>COMMON_STOCK</example>
    public string SecondaryInstrumentType { get; init; } = string.Empty;

    /// <summary>
    /// Latest known closing price.
    /// </summary>
    public double ClosingPrice { get; init; }

    /// <summary>
    /// Optional date for the closing price. Defaults to the current date when omitted.
    /// </summary>
    public DateOnly? ClosingPriceDate { get; init; }

    /// <summary>
    /// Source of the instrument record.
    /// </summary>
    /// <example>seed</example>
    public string Source { get; init; } = string.Empty;
}
