namespace PseudoMarkets.MarketData.Contracts.Quotes;

/// <summary>
/// Response containing major U.S. market index snapshots.
/// </summary>
public class IndicesResponse
{
    /// <summary>
    /// Collection of index snapshots.
    /// </summary>
    public IReadOnlyCollection<IndexSnapshotResponse> Indices { get; init; } = Array.Empty<IndexSnapshotResponse>();

    /// <summary>
    /// Data source, such as Yahoo Finance or Yahoo Finance Cached.
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the index response was generated or cached.
    /// </summary>
    public DateTimeOffset TimestampUtc { get; init; }
}
