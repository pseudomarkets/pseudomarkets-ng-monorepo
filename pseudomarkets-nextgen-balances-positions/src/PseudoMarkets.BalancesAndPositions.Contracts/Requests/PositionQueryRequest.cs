using System.ComponentModel.DataAnnotations;
using PseudoMarkets.BalancesAndPositions.Contracts.Enums;

namespace PseudoMarkets.BalancesAndPositions.Contracts.Requests;

public class PositionQueryRequest
{
    [Range(1_000_000_000, 9_999_999_999)]
    public long UserId { get; init; }

    public PositionView? View { get; init; }
}
