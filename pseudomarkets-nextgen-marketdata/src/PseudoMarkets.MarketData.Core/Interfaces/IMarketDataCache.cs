using PseudoMarkets.MarketData.Contracts.Quotes;

namespace PseudoMarkets.MarketData.Core.Interfaces;

public interface IMarketDataCache
{
    Task<QuoteResponse?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default);
    Task SetLatestQuoteAsync(QuoteResponse quote, CancellationToken cancellationToken = default);
    Task<DetailedQuoteResponse?> GetDetailedQuoteAsync(string symbol, string interval, CancellationToken cancellationToken = default);
    Task SetDetailedQuoteAsync(DetailedQuoteResponse quote, string interval, CancellationToken cancellationToken = default);
    Task<IndicesResponse?> GetUsMarketIndicesAsync(CancellationToken cancellationToken = default);
    Task SetUsMarketIndicesAsync(IndicesResponse indices, CancellationToken cancellationToken = default);
}
