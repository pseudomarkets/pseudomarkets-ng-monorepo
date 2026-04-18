using PseudoMarkets.MarketData.Contracts.Quotes;
using PseudoMarkets.MarketData.Core.Configuration;
using PseudoMarkets.MarketData.Core.Interfaces;

namespace PseudoMarkets.MarketData.Providers.Implementations;

public class TwelveDataMarketDataProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly TwelveDataConfiguration _configuration;

    public TwelveDataMarketDataProvider(HttpClient httpClient, TwelveDataConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;

        if (Uri.TryCreate(_configuration.BaseUrl, UriKind.Absolute, out var baseAddress))
        {
            _httpClient.BaseAddress = baseAddress;
        }
    }

    public Task<QuoteResponse?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<QuoteResponse?>(null);
    }

    public Task<DetailedQuoteResponse?> GetDetailedQuoteAsync(string symbol, string interval, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DetailedQuoteResponse?>(null);
    }

    public Task<IReadOnlyCollection<IndexSnapshotResponse>> GetUsMarketIndicesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<IndexSnapshotResponse> indices = Array.Empty<IndexSnapshotResponse>();
        return Task.FromResult(indices);
    }
}
