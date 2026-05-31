using PseudoMarkets.BalancesAndPositions.Contracts.Requests;
using PseudoMarkets.BalancesAndPositions.Contracts.Responses;

namespace PseudoMarkets.BalancesAndPositions.Core.Interfaces;

public interface IBalanceQueryService
{
    Task<BalanceQueryResponse> GetBalanceAsync(BalanceQueryRequest request, CancellationToken cancellationToken);
}
