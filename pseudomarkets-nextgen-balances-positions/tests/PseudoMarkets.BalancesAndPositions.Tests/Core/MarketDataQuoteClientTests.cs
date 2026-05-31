using System.Net;
using System.Net.Http.Json;
using Moq;
using NUnit.Framework;
using PseudoMarkets.BalancesAndPositions.Core.Clients;
using PseudoMarkets.BalancesAndPositions.Core.Interfaces;
using PseudoMarkets.MarketData.Contracts.Quotes;
using Shouldly;

namespace PseudoMarkets.BalancesAndPositions.Tests.Core;

[TestFixture]
public sealed class MarketDataQuoteClientTests
{
    [Test]
    public async Task GetQuoteAsync_ShouldReturnQuote_WhenMarketDataRespondsSuccessfully()
    {
        var tokenProvider = new Mock<ISystemTokenProvider>();
        tokenProvider.Setup(provider => provider.GetTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("token-1");

        var handler = new RecordingHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new QuoteResponse
                {
                    Symbol = "AAPL",
                    Price = 123.45m,
                    Source = "FinnHub",
                    TimestampUtc = DateTimeOffset.UtcNow
                })
            });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8081")
        };

        var sut = new MarketDataQuoteClient(httpClient, tokenProvider.Object);

        var response = await sut.GetQuoteAsync("AAPL", CancellationToken.None);

        response.IsQuoteAvailable.ShouldBeTrue();
        response.Price.ShouldBe(123.45m);
        handler.Requests.Single().Headers.Authorization!.Scheme.ShouldBe("Bearer");
        handler.Requests.Single().Headers.Authorization!.Parameter.ShouldBe("token-1");
    }

    [Test]
    public async Task GetQuoteAsync_ShouldReturnUnavailable_WhenDownstreamRequestFails()
    {
        var tokenProvider = new Mock<ISystemTokenProvider>();
        tokenProvider.Setup(provider => provider.GetTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("token-1");

        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.BadGateway));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8081")
        };

        var sut = new MarketDataQuoteClient(httpClient, tokenProvider.Object);

        var response = await sut.GetQuoteAsync("AAPL", CancellationToken.None);

        response.IsQuoteAvailable.ShouldBeFalse();
        response.WarningCode.ShouldBe("QUOTE_UNAVAILABLE");
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public RecordingHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_response);
        }
    }
}
