namespace PseudoMarkets.ReferenceData.TradingInstruments.Contracts.Instruments;

public sealed record UpdateClosingPriceRequest
{
    public double ClosingPrice { get; init; }
    public DateOnly? ClosingPriceDate { get; init; }
}
