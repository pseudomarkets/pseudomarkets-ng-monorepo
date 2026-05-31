using PseudoMarkets.BalancesAndPositions.Contracts.Enums;

namespace PseudoMarkets.BalancesAndPositions.Contracts.Responses;

public class PositionQueryResponse
{
    public required long RequestedUserId { get; init; }

    public required PositionView View { get; init; }

    public required IReadOnlyList<PositionSnapshotResponse> Positions { get; init; }

    public required IReadOnlyList<QueryWarningResponse> Warnings { get; init; }
}
