using PseudoMarkets.MarketData.Contracts.Quotes;
using PseudoMarkets.MarketData.Core.Exceptions;
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
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new MarketDataValidationException("A symbol is required.");
        }

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();

        var cachedQuote = await _marketDataCache.GetLatestQuoteAsync(normalizedSymbol, cancellationToken);
        if (cachedQuote is not null)
        {
            return cachedQuote;
        }

        var providerQuote = await _marketDataProvider.GetLatestQuoteAsync(normalizedSymbol, cancellationToken);
        if (providerQuote is not null)
        {
            await _marketDataCache.SetLatestQuoteAsync(providerQuote, cancellationToken);
        }

        return providerQuote;
    }

    public async Task<DetailedQuoteResponse?> GetDetailedQuoteAsync(string symbol, string interval = "1min", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new MarketDataValidationException("A symbol is required.");
        }

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var normalizedInterval = NormalizeInterval(interval);

        var cachedQuote = await _marketDataCache.GetDetailedQuoteAsync(normalizedSymbol, normalizedInterval, cancellationToken);
        if (cachedQuote is not null)
        {
            return cachedQuote;
        }

        var providerQuote = await _marketDataProvider.GetDetailedQuoteAsync(normalizedSymbol, normalizedInterval, cancellationToken);
        if (providerQuote is not null)
        {
            await _marketDataCache.SetDetailedQuoteAsync(providerQuote, normalizedInterval, cancellationToken);
        }

        return providerQuote;
    }

    public async Task<IndicesResponse?> GetUsMarketIndicesAsync(CancellationToken cancellationToken = default)
    {
        var cachedIndices = await _marketDataCache.GetUsMarketIndicesAsync(cancellationToken);
        if (cachedIndices is not null)
        {
            return cachedIndices;
        }

        var providerIndices = await _marketDataProvider.GetUsMarketIndicesAsync(cancellationToken);
        if (providerIndices is not null)
        {
            await _marketDataCache.SetUsMarketIndicesAsync(providerIndices, cancellationToken);
        }

        return providerIndices;
    }

    private static string NormalizeInterval(string? interval)
    {
        return string.IsNullOrWhiteSpace(interval) ? "1min" : interval.Trim();
    }
}
