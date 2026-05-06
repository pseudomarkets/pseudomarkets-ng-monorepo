using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PseudoMarkets.OrderExecution.Core.Exceptions;
using PseudoMarkets.OrderExecution.Core.Interfaces;
using PseudoMarkets.OrderExecution.Core.Models;
using PseudoMarkets.ReferenceData.TradingInstruments.Contracts.Instruments;

namespace PseudoMarkets.OrderExecution.Core.Clients;

public sealed class TradingInstrumentsClient : ITradingInstrumentsClient
{
    private readonly HttpClient _httpClient;
    private readonly ISystemTokenProvider _systemTokenProvider;

    public TradingInstrumentsClient(HttpClient httpClient, ISystemTokenProvider systemTokenProvider)
    {
        _httpClient = httpClient;
        _systemTokenProvider = systemTokenProvider;
    }

    public async Task<TradingInstrumentResponse> GetBySymbolAsync(string symbol, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/trading-instruments/{Uri.EscapeDataString(symbol)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _systemTokenProvider.GetTokenAsync(cancellationToken));

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new OrderExecutionValidationException(
                    OrderExecutionErrorCodes.UnsupportedSymbol,
                    $"Trading instrument '{symbol}' was not found.");
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new OrderExecutionDependencyException(
                    OrderExecutionErrorCodes.DownstreamUnauthorized,
                    "Trading Instruments rejected the Order Execution system account token.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new OrderExecutionDependencyException(
                    OrderExecutionErrorCodes.TradingInstrumentsUnavailable,
                    "Trading Instruments could not validate the submitted symbol.");
            }

            var instrument = await response.Content.ReadFromJsonAsync<TradingInstrumentResponse>(cancellationToken);
            return instrument ?? throw new OrderExecutionDependencyException(
                OrderExecutionErrorCodes.TradingInstrumentsUnavailable,
                "Trading Instruments returned an empty validation response.");
        }
        catch (OrderExecutionException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new OrderExecutionDependencyException(
                OrderExecutionErrorCodes.TradingInstrumentsUnavailable,
                "Trading Instruments could not be reached during order validation.",
                ex);
        }
    }
}
