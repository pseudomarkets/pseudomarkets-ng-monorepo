using Microsoft.EntityFrameworkCore;
using PseudoMarkets.BalancesAndPositions.Contracts.Enums;
using PseudoMarkets.BalancesAndPositions.Contracts.Requests;
using PseudoMarkets.BalancesAndPositions.Contracts.Responses;
using PseudoMarkets.BalancesAndPositions.Core.Exceptions;
using PseudoMarkets.BalancesAndPositions.Core.Interfaces;
using PseudoMarkets.Shared.Entities.Database;

namespace PseudoMarkets.BalancesAndPositions.Core.Services;

public sealed class BalanceQueryService : IBalanceQueryService
{
    private readonly PseudoMarketsDbContext _dbContext;

    public BalanceQueryService(PseudoMarketsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BalanceQueryResponse> GetBalanceAsync(BalanceQueryRequest request, CancellationToken cancellationToken)
    {
        var balance = await _dbContext.AccountBalances
            .SingleOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

        if (balance is null)
        {
            throw new BalancesAndPositionsNotFoundException($"No balance record was found for user '{request.UserId}'.");
        }

        var view = request.View ?? PositionView.All;

        return new BalanceQueryResponse
        {
            RequestedUserId = request.UserId,
            View = view,
            AggregateCashBalance = view == PositionView.All ? balance.CashBalance : null,
            SettledCashBalance = view is PositionView.All or PositionView.Settled ? balance.SettledCashBalance : null,
            UnsettledCashBalance = view is PositionView.All or PositionView.Unsettled ? balance.UnsettledCashBalance : null
        };
    }
}
