namespace PseudoMarkets.MarketData.Contracts.Quotes;

public class IndicesResponse
{
    public IReadOnlyCollection<IndexSnapshotResponse> Indices { get; init; } = Array.Empty<IndexSnapshotResponse>();
    public string Source { get; init; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; init; }
}
