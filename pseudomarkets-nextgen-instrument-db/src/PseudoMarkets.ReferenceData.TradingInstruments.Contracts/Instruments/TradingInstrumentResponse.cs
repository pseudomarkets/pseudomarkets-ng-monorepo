namespace PseudoMarkets.ReferenceData.TradingInstruments.Contracts.Instruments;

/// <summary>
/// Trading instrument reference-data response.
/// </summary>
public sealed record TradingInstrumentResponse
{
    /// <summary>
    /// Unique market symbol for the instrument.
    /// </summary>
    /// <example>AAPL</example>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable instrument description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Indicates whether the instrument can currently be traded on the platform.
    /// </summary>
    public bool TradingStatus { get; init; }

    /// <summary>
    /// Primary instrument type.
    /// </summary>
    public string PrimaryInstrumentType { get; init; } = string.Empty;

    /// <summary>
    /// Secondary instrument type.
    /// </summary>
    public string SecondaryInstrumentType { get; init; } = string.Empty;

    /// <summary>
    /// Latest known closing price.
    /// </summary>
    public double ClosingPrice { get; init; }

    /// <summary>
    /// Date associated with the latest known closing price.
    /// </summary>
    public DateOnly ClosingPriceDate { get; init; }

    /// <summary>
    /// Source of the instrument record.
    /// </summary>
    public string Source { get; init; } = string.Empty;
}
