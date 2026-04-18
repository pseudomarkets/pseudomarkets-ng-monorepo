using System.Globalization;
using System.Text.Json;
using PseudoMarkets.MarketData.Contracts.Quotes;
using PseudoMarkets.MarketData.Core.Configuration;
using PseudoMarkets.MarketData.Core.Exceptions;
using PseudoMarkets.MarketData.Core.Interfaces;
using PseudoMarkets.MarketData.Providers.Models;
using TwelveDataSharp;
using TwelveDataSharp.Interfaces;
using TwelveDataSharp.Library.ResponseModels;

namespace PseudoMarkets.MarketData.Providers.Implementations;

public class TwelveDataMarketDataProvider : IMarketDataProvider
{
    private const string TwelveDataSource = "Twelve Data";
    private const string TwelveDataTimeSeriesSource = "Twelve Data Time Series";
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient? _httpClient;
    private readonly TwelveDataConfiguration _configuration;
    private readonly ITwelveDataClient _twelveDataClient;

    public TwelveDataMarketDataProvider(HttpClient httpClient, TwelveDataConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _twelveDataClient = new TwelveDataClient(configuration.ApiKey, httpClient);
    }

    public TwelveDataMarketDataProvider(TwelveDataConfiguration configuration, ITwelveDataClient twelveDataClient)
    {
        _configuration = configuration;
        _twelveDataClient = twelveDataClient;
    }

    public async Task<QuoteResponse?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new MarketDataValidationException("A symbol is required.");
        }

        if (string.IsNullOrWhiteSpace(_configuration.ApiKey))
        {
            throw new MarketDataServiceException("The Twelve Data API key has not been configured.");
        }

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();

        try
        {
            var latestPrice = await _twelveDataClient.GetRealTimePriceAsync(normalizedSymbol);
            if (latestPrice.ResponseStatus == Enums.TwelveDataClientResponseStatus.TwelveDataApiError)
            {
                throw new MarketDataNotFoundException(latestPrice.ResponseMessage);
            }

            if (latestPrice.ResponseStatus == Enums.TwelveDataClientResponseStatus.TwelveDataSharpError)
            {
                throw new MarketDataDependencyException(latestPrice.ResponseMessage);
            }

            if (latestPrice.Price <= 0)
            {
                throw new MarketDataNotFoundException($"No quote was found for {normalizedSymbol}.");
            }

            return new QuoteResponse
            {
                Symbol = normalizedSymbol,
                Price = Convert.ToDecimal(latestPrice.Price),
                Source = TwelveDataSource,
                TimestampUtc = DateTimeOffset.UtcNow
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

    public async Task<DetailedQuoteResponse?> GetDetailedQuoteAsync(string symbol, string interval, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new MarketDataValidationException("A symbol is required.");
        }

        if (string.IsNullOrWhiteSpace(_configuration.ApiKey))
        {
            throw new MarketDataServiceException("The Twelve Data API key has not been configured.");
        }

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var normalizedInterval = string.IsNullOrWhiteSpace(interval) ? "1min" : interval.Trim();

        try
        {
            var detailedQuote = await _twelveDataClient.GetQuoteAsync(normalizedSymbol, normalizedInterval);
            if (detailedQuote.ResponseStatus == Enums.TwelveDataClientResponseStatus.TwelveDataApiError)
            {
                throw new MarketDataNotFoundException(detailedQuote.ResponseMessage);
            }

            if (detailedQuote.ResponseStatus == Enums.TwelveDataClientResponseStatus.TwelveDataSharpError)
            {
                throw new MarketDataDependencyException(detailedQuote.ResponseMessage);
            }

            if (string.IsNullOrWhiteSpace(detailedQuote.Symbol))
            {
                throw new MarketDataNotFoundException($"No detailed quote was found for {normalizedSymbol}.");
            }

            return new DetailedQuoteResponse
            {
                Symbol = normalizedSymbol,
                Name = detailedQuote.Name ?? string.Empty,
                Open = Convert.ToDecimal(detailedQuote.Open),
                High = Convert.ToDecimal(detailedQuote.High),
                Low = Convert.ToDecimal(detailedQuote.Low),
                Close = Convert.ToDecimal(detailedQuote.Close),
                Volume = detailedQuote.Volume,
                PreviousClose = Convert.ToDecimal(detailedQuote.PreviousClose),
                Change = Convert.ToDecimal(detailedQuote.Change),
                ChangePercentage = Convert.ToDecimal(detailedQuote.PercentChange),
                Source = TwelveDataSource,
                TimestampUtc = DateTimeOffset.UtcNow
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
        if (string.IsNullOrWhiteSpace(_configuration.ApiKey))
        {
            throw new MarketDataServiceException("The Twelve Data API key has not been configured.");
        }

        if (_httpClient is null)
        {
            throw new MarketDataServiceException("The Twelve Data HTTP client has not been configured.");
        }

        try
        {
            var endpoint = $"https://api.twelvedata.com/time_series?symbol=SPX,IXIC,DJI&interval=1min&apikey={Uri.EscapeDataString(_configuration.ApiKey)}";
            using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new MarketDataDependencyException($"Twelve Data returned HTTP {(int)response.StatusCode} while retrieving market indices.");
            }

            var indicesPayload = JsonSerializer.Deserialize<TwelveDataIndicesResponse>(responseBody, JsonSerializerOptions);

            var indexList = new List<IndexSnapshotResponse>
            {
                new()
                {
                    Name = "DOW",
                    Points = ParseIndexClose(indicesPayload?.Dow?.Values.FirstOrDefault()?.Close)
                },
                new()
                {
                    Name = "S&P 500",
                    Points = ParseIndexClose(indicesPayload?.Spx?.Values.FirstOrDefault()?.Close)
                },
                new()
                {
                    Name = "NASDAQ Composite",
                    Points = ParseIndexClose(indicesPayload?.Ixic?.Values.FirstOrDefault()?.Close)
                }
            };

            return new IndicesResponse
            {
                Indices = indexList,
                Source = TwelveDataTimeSeriesSource,
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

    private static decimal ParseIndexClose(string? value)
    {
        if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return 0m;
        }

        return parsedValue;
    }
}
