namespace PseudoMarkets.BalancesAndPositions.Contracts.Responses;

public class PositionSnapshotResponse
{
    public required string Symbol { get; init; }

    public decimal? AggregateQuantity { get; init; }

    public decimal? SettledQuantity { get; init; }

    public decimal? UnsettledQuantity { get; init; }

    public decimal? AggregateCostBasis { get; init; }

    public decimal? SettledCostBasis { get; init; }

    public decimal? UnsettledCostBasis { get; init; }

    public decimal? CurrentMarketPrice { get; init; }

    public decimal? AggregateMarketValue { get; init; }

    public decimal? SettledMarketValue { get; init; }

    public decimal? UnsettledMarketValue { get; init; }

    public decimal? AggregateUnrealizedGainLoss { get; init; }

    public decimal? SettledUnrealizedGainLoss { get; init; }

    public decimal? UnsettledUnrealizedGainLoss { get; init; }

    public required bool IsQuoteAvailable { get; init; }

    public string? QuoteWarningMessage { get; init; }
}
