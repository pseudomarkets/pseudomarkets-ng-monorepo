using PseudoMarkets.BalancesAndPositions.Contracts.Enums;

namespace PseudoMarkets.BalancesAndPositions.Contracts.Responses;

public class BalanceQueryResponse
{
    public required long RequestedUserId { get; init; }

    public required PositionView View { get; init; }

    public decimal? AggregateCashBalance { get; init; }

    public decimal? SettledCashBalance { get; init; }

    public decimal? UnsettledCashBalance { get; init; }
}
