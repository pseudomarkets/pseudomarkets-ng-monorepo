using PseudoMarkets.MarketData.Contracts.Quotes;

namespace PseudoMarkets.OrderExecution.Core.Interfaces;

public interface IMarketDataClient
{
    Task<QuoteResponse> GetQuoteAsync(string symbol, CancellationToken cancellationToken);
}
