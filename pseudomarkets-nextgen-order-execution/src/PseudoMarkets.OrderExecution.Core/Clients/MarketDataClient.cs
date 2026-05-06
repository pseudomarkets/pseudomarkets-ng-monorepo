using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PseudoMarkets.MarketData.Contracts.Quotes;
using PseudoMarkets.OrderExecution.Core.Exceptions;
using PseudoMarkets.OrderExecution.Core.Interfaces;
using PseudoMarkets.OrderExecution.Core.Models;

namespace PseudoMarkets.OrderExecution.Core.Clients;

public sealed class MarketDataClient : IMarketDataClient
{
    private readonly HttpClient _httpClient;
    private readonly ISystemTokenProvider _systemTokenProvider;

    public MarketDataClient(HttpClient httpClient, ISystemTokenProvider systemTokenProvider)
    {
        _httpClient = httpClient;
        _systemTokenProvider = systemTokenProvider;
    }

    public async Task<QuoteResponse> GetQuoteAsync(string symbol, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/MarketData/quote/{Uri.EscapeDataString(symbol)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _systemTokenProvider.GetTokenAsync(cancellationToken));

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new OrderExecutionDependencyException(
                    OrderExecutionErrorCodes.DownstreamUnauthorized,
                    "Market Data rejected the Order Execution system account token.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new OrderExecutionValidationException(
                    OrderExecutionErrorCodes.MarketDataUnavailable,
                    $"Market Data could not provide a quote for '{symbol}'.");
            }

            var quote = await response.Content.ReadFromJsonAsync<QuoteResponse>(cancellationToken);
            return quote ?? throw new OrderExecutionValidationException(
                OrderExecutionErrorCodes.MarketDataUnavailable,
                $"Market Data returned an empty quote for '{symbol}'.");
        }
        catch (OrderExecutionException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new OrderExecutionDependencyException(
                OrderExecutionErrorCodes.MarketDataUnavailable,
                "Market Data could not be reached during order validation.",
                ex);
        }
    }
}
