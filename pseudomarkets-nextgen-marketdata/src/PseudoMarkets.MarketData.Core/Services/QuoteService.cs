using PseudoMarkets.MarketData.Contracts.Quotes;
using PseudoMarkets.MarketData.Core.Interfaces;

namespace PseudoMarkets.MarketData.Core.Services;

public class QuoteService : IQuoteService
{
    private readonly IMarketDataCache _marketDataCache;
    private readonly IMarketDataProvider _marketDataProvider;

    public QuoteService(IMarketDataCache marketDataCache, IMarketDataProvider marketDataProvider)
    {
        _marketDataCache = marketDataCache;
        _marketDataProvider = marketDataProvider;
    }

    public async Task<QuoteResponse?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var cachedQuote = await _marketDataCache.GetLatestQuoteAsync(symbol, cancellationToken);
        if (cachedQuote is not null)
        {
            return cachedQuote;
        }

        var providerQuote = await _marketDataProvider.GetLatestQuoteAsync(symbol, cancellationToken);
        if (providerQuote is not null)
        {
            await _marketDataCache.SetLatestQuoteAsync(providerQuote, cancellationToken);
        }

        return providerQuote;
    }
}
