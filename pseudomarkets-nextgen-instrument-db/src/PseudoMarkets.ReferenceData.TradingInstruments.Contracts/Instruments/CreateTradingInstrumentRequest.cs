namespace PseudoMarkets.ReferenceData.TradingInstruments.Contracts.Instruments;

public sealed record CreateTradingInstrumentRequest
{
    public string Symbol { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string PrimaryInstrumentType { get; init; } = string.Empty;
    public string SecondaryInstrumentType { get; init; } = string.Empty;
    public double ClosingPrice { get; init; }
    public DateOnly? ClosingPriceDate { get; init; }
    public string Source { get; init; } = string.Empty;
}
