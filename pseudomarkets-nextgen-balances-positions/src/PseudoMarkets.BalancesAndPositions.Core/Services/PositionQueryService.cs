using Microsoft.EntityFrameworkCore;
using PseudoMarkets.BalancesAndPositions.Contracts.Enums;
using PseudoMarkets.BalancesAndPositions.Contracts.Requests;
using PseudoMarkets.BalancesAndPositions.Contracts.Responses;
using PseudoMarkets.BalancesAndPositions.Core.Interfaces;
using PseudoMarkets.Shared.Entities.Database;

namespace PseudoMarkets.BalancesAndPositions.Core.Services;

public sealed class PositionQueryService : IPositionQueryService
{
    private const string QuoteUnavailableCode = "QUOTE_UNAVAILABLE";
    private readonly PseudoMarketsDbContext _dbContext;
    private readonly IMarketDataQuoteClient _marketDataQuoteClient;

    public PositionQueryService(PseudoMarketsDbContext dbContext, IMarketDataQuoteClient marketDataQuoteClient)
    {
        _dbContext = dbContext;
        _marketDataQuoteClient = marketDataQuoteClient;
    }

    public async Task<PositionQueryResponse> GetPositionsAsync(PositionQueryRequest request, CancellationToken cancellationToken)
    {
        var view = request.View ?? PositionView.All;

        var positions = await _dbContext.Positions
            .Where(x => x.UserId == request.UserId && x.Quantity != 0m)
            .OrderBy(x => x.Symbol)
            .ToListAsync(cancellationToken);

        var results = new List<PositionSnapshotResponse>(positions.Count);
        var warnings = new List<QueryWarningResponse>();

        foreach (var position in positions)
        {
            var quote = await _marketDataQuoteClient.GetQuoteAsync(position.Symbol, cancellationToken);
            if (!quote.IsQuoteAvailable || quote.Price is null)
            {
                results.Add(new PositionSnapshotResponse
                {
                    Symbol = position.Symbol,
                    AggregateQuantity = view == PositionView.All ? position.Quantity : null,
                    SettledQuantity = view is PositionView.All or PositionView.Settled ? position.SettledQuantity : null,
                    UnsettledQuantity = view is PositionView.All or PositionView.Unsettled ? position.UnsettledQuantity : null,
                    AggregateCostBasis = view == PositionView.All ? position.CostBasisTotal : null,
                    SettledCostBasis = view is PositionView.All or PositionView.Settled ? position.SettledCostBasisTotal : null,
                    UnsettledCostBasis = view is PositionView.All or PositionView.Unsettled ? position.UnsettledCostBasisTotal : null,
                    CurrentMarketPrice = null,
                    AggregateMarketValue = null,
                    SettledMarketValue = null,
                    UnsettledMarketValue = null,
                    AggregateUnrealizedGainLoss = null,
                    SettledUnrealizedGainLoss = null,
                    UnsettledUnrealizedGainLoss = null,
                    IsQuoteAvailable = false,
                    QuoteWarningMessage = quote.WarningMessage
                });

                warnings.Add(new QueryWarningResponse
                {
                    Code = quote.WarningCode ?? QuoteUnavailableCode,
                    Message = quote.WarningMessage ?? $"Quote data is unavailable for symbol '{position.Symbol}'.",
                    Symbol = position.Symbol
                });

                continue;
            }

            var currentPrice = quote.Price.Value;
            var aggregateMarketValue = position.Quantity * currentPrice;
            var settledMarketValue = position.SettledQuantity * currentPrice;
            var unsettledMarketValue = position.UnsettledQuantity * currentPrice;

            results.Add(new PositionSnapshotResponse
            {
                Symbol = position.Symbol,
                AggregateQuantity = view == PositionView.All ? position.Quantity : null,
                SettledQuantity = view is PositionView.All or PositionView.Settled ? position.SettledQuantity : null,
                UnsettledQuantity = view is PositionView.All or PositionView.Unsettled ? position.UnsettledQuantity : null,
                AggregateCostBasis = view == PositionView.All ? position.CostBasisTotal : null,
                SettledCostBasis = view is PositionView.All or PositionView.Settled ? position.SettledCostBasisTotal : null,
                UnsettledCostBasis = view is PositionView.All or PositionView.Unsettled ? position.UnsettledCostBasisTotal : null,
                CurrentMarketPrice = currentPrice,
                AggregateMarketValue = view == PositionView.All ? aggregateMarketValue : null,
                SettledMarketValue = view is PositionView.All or PositionView.Settled ? settledMarketValue : null,
                UnsettledMarketValue = view is PositionView.All or PositionView.Unsettled ? unsettledMarketValue : null,
                AggregateUnrealizedGainLoss = view == PositionView.All ? aggregateMarketValue - position.CostBasisTotal : null,
                SettledUnrealizedGainLoss = view is PositionView.All or PositionView.Settled ? settledMarketValue - position.SettledCostBasisTotal : null,
                UnsettledUnrealizedGainLoss = view is PositionView.All or PositionView.Unsettled ? unsettledMarketValue - position.UnsettledCostBasisTotal : null,
                IsQuoteAvailable = true,
                QuoteWarningMessage = null
            });
        }

        return new PositionQueryResponse
        {
            RequestedUserId = request.UserId,
            View = view,
            Positions = results,
            Warnings = warnings
        };
    }
}
