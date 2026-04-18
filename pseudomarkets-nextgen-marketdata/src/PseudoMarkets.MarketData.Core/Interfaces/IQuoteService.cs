using PseudoMarkets.MarketData.Contracts.Quotes;

namespace PseudoMarkets.MarketData.Core.Interfaces;

public interface IQuoteService
{
    Task<QuoteResponse?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default);
    Task<DetailedQuoteResponse?> GetDetailedQuoteAsync(string symbol, string interval = "1min", CancellationToken cancellationToken = default);
    Task<IndicesResponse?> GetUsMarketIndicesAsync(CancellationToken cancellationToken = default);
}
