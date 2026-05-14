using System.Globalization;
using System.Net;
using System.Text.Json;
using FinnHubSharp.Interfaces;
using FinnHubSharp.Implementations;
using FinnHubSharp.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PseudoMarkets.MarketData.Contracts.Quotes;
using PseudoMarkets.MarketData.Core.Configuration;
using PseudoMarkets.MarketData.Core.Exceptions;
using PseudoMarkets.MarketData.Core.Interfaces;
using PseudoMarkets.MarketData.Providers.Models;

namespace PseudoMarkets.MarketData.Providers.Implementations;

public class FinnHubMarketDataProvider : IMarketDataProvider
{
    private const string FinnHubSource = "Finnhub";
    private const string YahooFinanceSource = "Yahoo Finance";
    private const string Sp500Symbol = "^GSPC";
    private const string DowJonesSymbol = "^DJI";
    private const string NasdaqCompositeSymbol = "^IXIC";
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly FinnHubConfiguration _configuration;
    private readonly IFinnHubClient _finnHubClient;

    [ActivatorUtilitiesConstructor]
    public FinnHubMarketDataProvider(HttpClient httpClient, FinnHubConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _finnHubClient = new FinnHubClient(
            httpClient,
            new FinnHubSharpConfiguration
            {
                ApiKey = configuration.ApiKey,
                BaseUrl = configuration.BaseUrl
            });
    }

    public FinnHubMarketDataProvider(HttpClient httpClient, FinnHubConfiguration configuration, IFinnHubClient finnHubClient)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _finnHubClient = finnHubClient;
    }

    public async Task<QuoteResponse?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        EnsureApiKeyConfigured();

        try
        {
            var response = await _finnHubClient.GetQuoteAsync(normalizedSymbol);
            EnsureSuccessfulQuoteResponse(response.ResponseCode, response.ErrorMessage, normalizedSymbol);

            if (response.Quote is null || response.Quote.CurrentPrice <= 0)
            {
                throw new MarketDataNotFoundException($"No quote was found for {normalizedSymbol}.");
            }

            return new QuoteResponse
            {
                Symbol = normalizedSymbol,
                Price = Convert.ToDecimal(response.Quote.CurrentPrice, CultureInfo.InvariantCulture),
                Source = FinnHubSource,
                TimestampUtc = ToTimestamp(response.Quote.Timestamp)
            };
        }
        catch (MarketDataNotFoundException)
        {
            throw;
        }
        catch (MarketDataValidationException)
        {
            throw;
        }
        catch (MarketDataDependencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new MarketDataServiceException($"An unexpected error occurred while retrieving {normalizedSymbol}.", ex);
        }
    }

    public async Task<DetailedQuoteResponse?> GetDetailedQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        EnsureApiKeyConfigured();

        try
        {
            var response = await _finnHubClient.GetQuoteAsync(normalizedSymbol);
            EnsureSuccessfulQuoteResponse(response.ResponseCode, response.ErrorMessage, normalizedSymbol);

            if (response.Quote is null || response.Quote.CurrentPrice <= 0)
            {
                throw new MarketDataNotFoundException($"No detailed quote was found for {normalizedSymbol}.");
            }

            var name = await TryGetSymbolDescriptionAsync(normalizedSymbol);

            var currentPrice = Convert.ToDecimal(response.Quote.CurrentPrice, CultureInfo.InvariantCulture);
            var previousClose = Convert.ToDecimal(response.Quote.PreviousClose, CultureInfo.InvariantCulture);
            var change = currentPrice - previousClose;
            var changePercentage = previousClose == 0m
                ? 0m
                : decimal.Round((change / previousClose) * 100m, 4, MidpointRounding.AwayFromZero);

            return new DetailedQuoteResponse
            {
                Symbol = normalizedSymbol,
                Name = name,
                Open = Convert.ToDecimal(response.Quote.Open, CultureInfo.InvariantCulture),
                High = Convert.ToDecimal(response.Quote.High, CultureInfo.InvariantCulture),
                Low = Convert.ToDecimal(response.Quote.Low, CultureInfo.InvariantCulture),
                Close = currentPrice,
                PreviousClose = previousClose,
                Change = change,
                ChangePercentage = changePercentage,
                Source = FinnHubSource,
                TimestampUtc = ToTimestamp(response.Quote.Timestamp)
            };
        }
        catch (MarketDataNotFoundException)
        {
            throw;
        }
        catch (MarketDataValidationException)
        {
            throw;
        }
        catch (MarketDataDependencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new MarketDataServiceException($"An unexpected error occurred while retrieving the detailed quote for {normalizedSymbol}.", ex);
        }
    }

    public async Task<IndicesResponse?> GetUsMarketIndicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var priceTasks = new[]
            {
                GetYahooIndexPriceAsync(Sp500Symbol, "S&P 500", cancellationToken),
                GetYahooIndexPriceAsync(DowJonesSymbol, "Dow Jones Industrial Average", cancellationToken),
                GetYahooIndexPriceAsync(NasdaqCompositeSymbol, "NASDAQ Composite", cancellationToken)
            };

            var indices = await Task.WhenAll(priceTasks);

            return new IndicesResponse
            {
                Indices = indices,
                Source = YahooFinanceSource,
                TimestampUtc = DateTimeOffset.UtcNow
            };
        }
        catch (MarketDataDependencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new MarketDataServiceException("An unexpected error occurred while retrieving U.S. market indices.", ex);
        }
    }

    private void EnsureApiKeyConfigured()
    {
        if (string.IsNullOrWhiteSpace(_configuration.ApiKey))
        {
            throw new MarketDataServiceException("The Finnhub API key has not been configured.");
        }
    }

    private static string NormalizeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new MarketDataValidationException("A symbol is required.");
        }

        return symbol.Trim().ToUpperInvariant();
    }

    private static void EnsureSuccessfulQuoteResponse(int responseCode, string? errorMessage, string symbol)
    {
        if (responseCode is >= 200 and < 300)
        {
            return;
        }

        if (responseCode == (int)HttpStatusCode.NotFound)
        {
            throw new MarketDataNotFoundException($"No quote was found for {symbol}.");
        }

        throw new MarketDataDependencyException(
            string.IsNullOrWhiteSpace(errorMessage)
                ? $"Finnhub returned HTTP {responseCode} while retrieving {symbol}."
                : errorMessage);
    }

    private async Task<string> TryGetSymbolDescriptionAsync(string normalizedSymbol)
    {
        try
        {
            var response = await _finnHubClient.GetSymbolInfoAsync(normalizedSymbol);
            if (response.ResponseCode is < 200 or >= 300 || response.SymbolInfo?.Result is null)
            {
                return string.Empty;
            }

            var match = response.SymbolInfo.Result.FirstOrDefault(result =>
                string.Equals(result.Symbol, normalizedSymbol, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(result.DisplaySymbol, normalizedSymbol, StringComparison.OrdinalIgnoreCase));

            return match?.Description?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private async Task<IndexSnapshotResponse> GetYahooIndexPriceAsync(
        string symbol,
        string displayName,
        CancellationToken cancellationToken)
    {
        var endpoint = $"https://query2.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        request.Headers.Referrer = new Uri("https://finance.yahoo.com/");
        request.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new MarketDataDependencyException(
                $"Yahoo Finance returned HTTP {(int)response.StatusCode} while retrieving {displayName}.");
        }

        var payload = JsonSerializer.Deserialize<YahooFinanceChartResponse>(responseBody, JsonSerializerOptions);
        var regularMarketPrice = payload?.Chart?.Result.FirstOrDefault()?.Meta?.RegularMarketPrice;

        if (regularMarketPrice is null)
        {
            throw new MarketDataDependencyException(
                $"Yahoo Finance did not return a regularMarketPrice for {displayName}.");
        }

        return new IndexSnapshotResponse
        {
            Name = displayName,
            Points = regularMarketPrice.Value
        };
    }

    private static DateTimeOffset ToTimestamp(long unixTimestamp)
    {
        return unixTimestamp > 0
            ? DateTimeOffset.FromUnixTimeSeconds(unixTimestamp)
            : DateTimeOffset.UtcNow;
    }
}
