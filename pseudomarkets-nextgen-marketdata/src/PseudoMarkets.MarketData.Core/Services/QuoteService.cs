using PseudoMarkets.MarketData.Contracts.Quotes;
using PseudoMarkets.MarketData.Core.Exceptions;
using PseudoMarkets.MarketData.Core.Interfaces;

namespace PseudoMarkets.MarketData.Core.Services;

public class QuoteService : IQuoteService
{
    private const string CachedSourceSuffix = " Cached";
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
            return CreateCachedQuoteResponse(cachedQuote);
        }

        var providerQuote = await _marketDataProvider.GetLatestQuoteAsync(normalizedSymbol, cancellationToken);
        if (providerQuote is not null)
        {
            await _marketDataCache.SetLatestQuoteAsync(providerQuote, cancellationToken);
        }

        return providerQuote;
    }

    public async Task<DetailedQuoteResponse?> GetDetailedQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new MarketDataValidationException("A symbol is required.");
        }

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();

        var cachedQuote = await _marketDataCache.GetDetailedQuoteAsync(normalizedSymbol, cancellationToken);
        if (cachedQuote is not null)
        {
            return CreateCachedDetailedQuoteResponse(cachedQuote);
        }

        var providerQuote = await _marketDataProvider.GetDetailedQuoteAsync(normalizedSymbol, cancellationToken);
        if (providerQuote is not null)
        {
            await _marketDataCache.SetDetailedQuoteAsync(providerQuote, cancellationToken);
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

    private static QuoteResponse CreateCachedQuoteResponse(QuoteResponse quote)
    {
        return new QuoteResponse
        {
            Symbol = quote.Symbol,
            Price = quote.Price,
            Source = AppendCachedSuffix(quote.Source),
            TimestampUtc = quote.TimestampUtc
        };
    }

    private static DetailedQuoteResponse CreateCachedDetailedQuoteResponse(DetailedQuoteResponse quote)
    {
        return new DetailedQuoteResponse
        {
            Symbol = quote.Symbol,
            Name = quote.Name,
            Open = quote.Open,
            High = quote.High,
            Low = quote.Low,
            Close = quote.Close,
            PreviousClose = quote.PreviousClose,
            Change = quote.Change,
            ChangePercentage = quote.ChangePercentage,
            Source = AppendCachedSuffix(quote.Source),
            TimestampUtc = quote.TimestampUtc
        };
    }

    private static string AppendCachedSuffix(string source)
    {
        if (string.IsNullOrWhiteSpace(source) ||
            source.EndsWith(CachedSourceSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return source;
        }

        return $"{source}{CachedSourceSuffix}";
    }
}
