using System.Net.Http.Headers;
using System.Net.Http.Json;
using PseudoMarkets.OrderExecution.Contracts.Enums;
using PseudoMarkets.OrderExecution.Contracts.Orders;
using PseudoMarkets.Platform.Batch.Host.Interfaces;
using PseudoMarkets.Shared.Entities.Entities.OrderExecution;

namespace PseudoMarkets.Platform.Batch.Host.Clients;

internal sealed class OrderExecutionClient : IOrderExecutionClient
{
    private readonly HttpClient _httpClient;
    private readonly IQueuedOrderExecutionTokenProvider _tokenProvider;

    public OrderExecutionClient(
        HttpClient httpClient,
        IQueuedOrderExecutionTokenProvider tokenProvider)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
    }

    public async Task<SubmitOrderResponse> SubmitQueuedOrderAsync(
        QueuedOrderEntity queuedOrder,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new SubmitOrderRequest
            {
                UserId = queuedOrder.UserId,
                Symbol = queuedOrder.Symbol,
                Side = Enum.Parse<OrderSide>(queuedOrder.OrderSide, ignoreCase: true),
                Quantity = queuedOrder.Quantity,
                OrderType = Enum.Parse<OrderType>(queuedOrder.OrderType, ignoreCase: true)
            })
        };

        var token = await _tokenProvider.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Order Execution returned {(int)response.StatusCode} when processing queued order {queuedOrder.OrderId}: {errorContent}");
        }

        var payload = await response.Content.ReadFromJsonAsync<SubmitOrderResponse>(cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException(
                $"Order Execution returned an empty response for queued order {queuedOrder.OrderId}.");
        }

        return payload;
    }
}
