using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using PseudoMarkets.Platform.Batch.Host.Clients;
using PseudoMarkets.Platform.Batch.Host.Interfaces;
using PseudoMarkets.Shared.Entities.Entities.OrderExecution;
using Shouldly;

namespace PseudoMarkets.Platform.Batch.Tests.Host;

[TestFixture]
public sealed class OrderExecutionClientTests
{
    [Test]
    public async Task SubmitQueuedOrderAsync_ShouldSendOriginalQueuedOrderUserIdAndBearerToken()
    {
        var handler = new RecordingHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8084")
        };

        var sut = new OrderExecutionClient(httpClient, new FakeTokenProvider("system-token"));
        var queuedOrder = new QueuedOrderEntity
        {
            OrderId = Guid.NewGuid(),
            UserId = 1000000007,
            Symbol = "AAPL",
            OrderSide = "Buy",
            OrderType = "Market",
            Quantity = 2,
            Status = "Pending",
            QueueReason = "AfterClose",
            SubmittedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await sut.SubmitQueuedOrderAsync(queuedOrder, CancellationToken.None);

        handler.RequestUri!.AbsolutePath.ShouldBe("/api/orders");
        handler.AuthorizationHeader.ShouldBe("Bearer system-token");
        handler.RequestBody.ShouldContain("\"userId\":1000000007");
        handler.RequestBody.ShouldContain("\"symbol\":\"AAPL\"");
    }

    private sealed class FakeTokenProvider : IQueuedOrderExecutionTokenProvider
    {
        private readonly string _token;

        public FakeTokenProvider(string token)
        {
            _token = token;
        }

        public Task<string> GetTokenAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_token);
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string AuthorizationHeader { get; private set; } = string.Empty;
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            AuthorizationHeader = request.Headers.Authorization?.ToString() ?? string.Empty;
            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "disposition": 1,
                      "orderId": "11111111-1111-1111-1111-111111111111",
                      "executionId": "22222222-2222-2222-2222-222222222222",
                      "transactionId": "33333333-3333-3333-3333-333333333333",
                      "postingBatchId": "44444444-4444-4444-4444-444444444444",
                      "userId": 1000000007,
                      "symbol": "AAPL",
                      "side": 1,
                      "orderType": 1,
                      "quantity": 2,
                      "fillPrice": 100.0,
                      "grossAmount": 200.0,
                      "fees": 0.0,
                      "netAmount": 200.0,
                      "status": 2,
                      "submittedAtUtc": "2026-05-17T00:00:00Z",
                      "executedAtUtc": "2026-05-17T00:01:00Z"
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
