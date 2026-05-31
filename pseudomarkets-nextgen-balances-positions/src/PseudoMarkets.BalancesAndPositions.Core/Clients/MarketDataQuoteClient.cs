using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PseudoMarkets.BalancesAndPositions.Core.Exceptions;
using PseudoMarkets.BalancesAndPositions.Core.Interfaces;
using PseudoMarkets.BalancesAndPositions.Core.Models;
using PseudoMarkets.MarketData.Contracts.Quotes;

namespace PseudoMarkets.BalancesAndPositions.Core.Clients;

public sealed class MarketDataQuoteClient : IMarketDataQuoteClient
{
    private const string QuoteUnavailableCode = "QUOTE_UNAVAILABLE";
    private readonly HttpClient _httpClient;
    private readonly ISystemTokenProvider _systemTokenProvider;

    public MarketDataQuoteClient(HttpClient httpClient, ISystemTokenProvider systemTokenProvider)
    {
        _httpClient = httpClient;
        _systemTokenProvider = systemTokenProvider;
    }

    public async Task<QuoteLookupResult> GetQuoteAsync(string symbol, CancellationToken cancellationToken)
    {
        if (_httpClient.BaseAddress is null)
        {
            return new QuoteLookupResult(false, null, QuoteUnavailableCode, "Market Data base URL is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/marketdata/quote/{Uri.EscapeDataString(symbol)}");

        try
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _systemTokenProvider.GetTokenAsync(cancellationToken));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return new QuoteLookupResult(false, null, QuoteUnavailableCode, $"Market Data rejected quote access for '{symbol}'.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return new QuoteLookupResult(false, null, QuoteUnavailableCode, $"Market Data could not provide a quote for '{symbol}'.");
            }

            var quote = await response.Content.ReadFromJsonAsync<QuoteResponse>(cancellationToken);
            if (quote is null)
            {
                return new QuoteLookupResult(false, null, QuoteUnavailableCode, $"Market Data returned an empty quote for '{symbol}'.");
            }

            return new QuoteLookupResult(true, quote.Price, null, null);
        }
        catch (BalancesAndPositionsDependencyException)
        {
            return new QuoteLookupResult(false, null, QuoteUnavailableCode, $"Market Data authorization is unavailable for '{symbol}'.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new QuoteLookupResult(false, null, QuoteUnavailableCode, $"Market Data could not be reached for '{symbol}'.");
        }
    }
}
