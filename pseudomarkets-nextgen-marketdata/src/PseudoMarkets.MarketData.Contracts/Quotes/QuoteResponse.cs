namespace PseudoMarkets.MarketData.Contracts.Quotes;

public class QuoteResponse
{
    public required string Symbol { get; init; }
    public decimal Price { get; init; }
    public string Source { get; init; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; init; }
}
