using PseudoMarkets.MarketData.Contracts.Quotes;

namespace PseudoMarkets.MarketData.Core.Interfaces;

public interface IMarketDataCache
{
    Task<QuoteResponse?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default);
    Task SetLatestQuoteAsync(QuoteResponse quote, CancellationToken cancellationToken = default);
}
