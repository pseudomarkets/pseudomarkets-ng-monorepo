namespace PseudoMarkets.Shared.Entities.Entities.ReferenceData;

public sealed class TradingInstrumentEntity
{
    public string Symbol { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool TradingStatus { get; set; } = true;
    public string PrimaryInstrumentType { get; set; } = string.Empty;
    public string SecondaryInstrumentType { get; set; } = string.Empty;
    public double ClosingPrice { get; set; }
    public DateOnly ClosingPriceDate { get; set; }
    public string Source { get; set; } = string.Empty;
}
