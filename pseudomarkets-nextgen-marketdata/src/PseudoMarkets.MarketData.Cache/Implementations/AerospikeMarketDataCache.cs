using PseudoMarkets.MarketData.Contracts.Quotes;
using PseudoMarkets.MarketData.Core.Configuration;
using PseudoMarkets.MarketData.Core.Interfaces;

namespace PseudoMarkets.MarketData.Cache.Implementations;

public class AerospikeMarketDataCache : IMarketDataCache
{
    private readonly AerospikeConfiguration _aerospikeConfiguration;
    private readonly MarketDataCacheConfiguration _cacheConfiguration;

    public AerospikeMarketDataCache(
        AerospikeConfiguration aerospikeConfiguration,
        MarketDataCacheConfiguration cacheConfiguration)
    {
        _aerospikeConfiguration = aerospikeConfiguration;
        _cacheConfiguration = cacheConfiguration;
    }

    public Task<QuoteResponse?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<QuoteResponse?>(null);
    }

    public Task SetLatestQuoteAsync(QuoteResponse quote, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
