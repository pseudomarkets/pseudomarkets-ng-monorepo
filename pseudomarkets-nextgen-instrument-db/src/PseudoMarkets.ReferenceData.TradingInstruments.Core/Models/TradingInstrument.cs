namespace PseudoMarkets.ReferenceData.TradingInstruments.Core.Models;

public sealed record TradingInstrument
{
    public string Symbol { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool TradingStatus { get; init; } = true;
    public string PrimaryInstrumentType { get; init; } = string.Empty;
    public string SecondaryInstrumentType { get; init; } = string.Empty;
    public double ClosingPrice { get; init; }
    public DateOnly ClosingPriceDate { get; init; }
    public string Source { get; init; } = string.Empty;
}
