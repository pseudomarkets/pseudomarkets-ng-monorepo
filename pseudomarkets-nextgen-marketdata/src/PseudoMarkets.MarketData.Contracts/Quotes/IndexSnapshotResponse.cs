namespace PseudoMarkets.MarketData.Contracts.Quotes;

/// <summary>
/// Snapshot value for a market index.
/// </summary>
public class IndexSnapshotResponse
{
    /// <summary>
    /// Market index display name.
    /// </summary>
    /// <example>S&amp;P 500</example>
    public required string Name { get; init; }

    /// <summary>
    /// Current index point value.
    /// </summary>
    public decimal Points { get; init; }
}
