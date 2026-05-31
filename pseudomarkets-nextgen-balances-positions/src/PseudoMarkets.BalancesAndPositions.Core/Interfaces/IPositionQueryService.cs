using PseudoMarkets.BalancesAndPositions.Contracts.Requests;
using PseudoMarkets.BalancesAndPositions.Contracts.Responses;

namespace PseudoMarkets.BalancesAndPositions.Core.Interfaces;

public interface IPositionQueryService
{
    Task<PositionQueryResponse> GetPositionsAsync(PositionQueryRequest request, CancellationToken cancellationToken);
}
