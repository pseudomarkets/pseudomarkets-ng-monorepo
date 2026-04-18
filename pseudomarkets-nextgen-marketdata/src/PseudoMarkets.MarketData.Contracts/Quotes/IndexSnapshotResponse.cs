namespace PseudoMarkets.MarketData.Contracts.Quotes;

public class IndexSnapshotResponse
{
    public required string Name { get; init; }
    public decimal Points { get; init; }
}
