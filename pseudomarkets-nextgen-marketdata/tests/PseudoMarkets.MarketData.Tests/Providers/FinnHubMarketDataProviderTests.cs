using System.Net;
using System.Net.Http.Json;
using FinnHubSharp.Interfaces;
using FinnHubSharp.Models.Response.FinnHub;
using FinnHubSharp.Models.Response.Raw;
using Moq;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.MarketData.Core.Configuration;
using PseudoMarkets.MarketData.Core.Exceptions;
using PseudoMarkets.MarketData.Providers.Implementations;

namespace PseudoMarkets.MarketData.Tests.Providers;

[TestFixture]
public class FinnHubMarketDataProviderTests
{
    private FinnHubConfiguration _configuration = null!;
    private Mock<IFinnHubClient> _finnHubClient = null!;

    [SetUp]
    public void SetUp()
    {
        _configuration = new FinnHubConfiguration
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://finnhub.io/api/v1"
        };
        _finnHubClient = new Mock<IFinnHubClient>();
    }

    [Test]
    public async Task GetLatestQuoteAsync_ShouldMapQuote_WhenLibraryReturnsPrice()
    {
        _finnHubClient
            .Setup(x => x.GetQuoteAsync("AAPL"))
            .ReturnsAsync(new FinnHubQuote
            {
                ResponseCode = 200,
                Quote = new Quote
                {
                    CurrentPrice = 177.12,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            });
        _finnHubClient
            .Setup(x => x.GetSymbolInfoAsync("AAPL"))
            .ReturnsAsync(new FinnHubSymbolInfo
            {
                ResponseCode = 200,
                SymbolInfo = new SymbolInfo
                {
                    Result =
                    [
                        new Result
                        {
                            Symbol = "AAPL",
                            DisplaySymbol = "AAPL",
                            Description = "Apple Inc."
                        }
                    ]
                }
            });

        var sut = CreateSut();

        var result = await sut.GetLatestQuoteAsync("aapl");

        result.ShouldNotBeNull();
        result.Symbol.ShouldBe("AAPL");
        result.Price.ShouldBe(177.12m);
        result.Source.ShouldBe("Finnhub");
    }

    [Test]
    public async Task GetLatestQuoteAsync_ShouldThrowDependencyException_WhenLibraryReturnsHttpError()
    {
        _finnHubClient
            .Setup(x => x.GetQuoteAsync("AAPL"))
            .ReturnsAsync(new FinnHubQuote
            {
                ResponseCode = 429,
                ErrorMessage = "Rate limit exceeded"
            });

        var sut = CreateSut();

        var ex = await Should.ThrowAsync<MarketDataDependencyException>(() => sut.GetLatestQuoteAsync("AAPL"));

        ex.Message.ShouldBe("Rate limit exceeded");
    }

    [Test]
    public async Task GetDetailedQuoteAsync_ShouldMapSupportedFields_WhenLibraryReturnsQuote()
    {
        _finnHubClient
            .Setup(x => x.GetQuoteAsync("AAPL"))
            .ReturnsAsync(new FinnHubQuote
            {
                ResponseCode = 200,
                Quote = new Quote
                {
                    CurrentPrice = 105,
                    Open = 100,
                    High = 110,
                    Low = 95,
                    PreviousClose = 99,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            });
        _finnHubClient
            .Setup(x => x.GetSymbolInfoAsync("AAPL"))
            .ReturnsAsync(new FinnHubSymbolInfo
            {
                ResponseCode = 200,
                SymbolInfo = new SymbolInfo
                {
                    Result =
                    [
                        new Result
                        {
                            Symbol = "AAPL",
                            DisplaySymbol = "AAPL",
                            Description = "Apple Inc."
                        }
                    ]
                }
            });

        var sut = CreateSut();

        var result = await sut.GetDetailedQuoteAsync("AAPL");

        result.ShouldNotBeNull();
        result.Symbol.ShouldBe("AAPL");
        result.Name.ShouldBe("Apple Inc.");
        result.Open.ShouldBe(100m);
        result.High.ShouldBe(110m);
        result.Low.ShouldBe(95m);
        result.Close.ShouldBe(105m);
        result.PreviousClose.ShouldBe(99m);
        result.Change.ShouldBe(6m);
        result.ChangePercentage.ShouldBe(6.0606m);
    }

    [Test]
    public async Task GetUsMarketIndicesAsync_ShouldMapYahooRegularMarketPrices()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            var symbol = WebUtility.UrlDecode(request.RequestUri!.Segments.Last());
            var price = symbol switch
            {
                "^GSPC" => 5123.45m,
                "^DJI" => 38999.12m,
                "^IXIC" => 16234.78m,
                _ => 0m
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    chart = new
                    {
                        result = new[]
                        {
                            new
                            {
                                meta = new
                                {
                                    regularMarketPrice = price
                                }
                            }
                        }
                    }
                })
            };
        });

        var httpClient = new HttpClient(handler);
        var sut = new FinnHubMarketDataProvider(httpClient, _configuration, _finnHubClient.Object);

        var result = await sut.GetUsMarketIndicesAsync();

        result.ShouldNotBeNull();
        result.Source.ShouldBe("Yahoo Finance");
        result.Indices.Count.ShouldBe(3);
        result.Indices.First(x => x.Name == "S&P 500").Points.ShouldBe(5123.45m);
        result.Indices.First(x => x.Name == "Dow Jones Industrial Average").Points.ShouldBe(38999.12m);
        result.Indices.First(x => x.Name == "NASDAQ Composite").Points.ShouldBe(16234.78m);
    }

    private FinnHubMarketDataProvider CreateSut()
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        return new FinnHubMarketDataProvider(httpClient, _configuration, _finnHubClient.Object);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
