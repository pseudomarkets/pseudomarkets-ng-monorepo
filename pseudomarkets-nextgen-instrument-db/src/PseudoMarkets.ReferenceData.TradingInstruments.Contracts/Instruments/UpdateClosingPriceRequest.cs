namespace PseudoMarkets.ReferenceData.TradingInstruments.Contracts.Instruments;

/// <summary>
/// Request used to update a trading instrument closing price.
/// </summary>
public sealed record UpdateClosingPriceRequest
{
    /// <summary>
    /// Updated closing price.
    /// </summary>
    public double ClosingPrice { get; init; }

    /// <summary>
    /// Optional date for the updated closing price. Defaults to the current date when omitted.
    /// </summary>
    public DateOnly? ClosingPriceDate { get; init; }
}
