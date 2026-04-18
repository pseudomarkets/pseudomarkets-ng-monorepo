namespace PseudoMarkets.MarketData.Contracts.Quotes;

public class DetailedQuoteResponse
{
    public required string Symbol { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }
    public long Volume { get; init; }
    public decimal PreviousClose { get; init; }
    public decimal Change { get; init; }
    public decimal ChangePercentage { get; init; }
    public string Source { get; init; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; init; }
}
