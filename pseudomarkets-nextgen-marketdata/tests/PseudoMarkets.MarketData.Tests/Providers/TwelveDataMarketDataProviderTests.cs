using Moq;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.MarketData.Core.Configuration;
using PseudoMarkets.MarketData.Core.Exceptions;
using PseudoMarkets.MarketData.Providers.Implementations;
using TwelveDataSharp.Interfaces;
using TwelveDataSharp.Library.ResponseModels;

namespace PseudoMarkets.MarketData.Tests.Providers;

[TestFixture]
public class TwelveDataMarketDataProviderTests
{
    private TwelveDataConfiguration _configuration = null!;
    private Mock<ITwelveDataClient> _twelveDataClient = null!;

    [SetUp]
    public void SetUp()
    {
        _configuration = new TwelveDataConfiguration
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.twelvedata.com"
        };
        _twelveDataClient = new Mock<ITwelveDataClient>();
    }

    [Test]
    public async Task GetLatestQuoteAsync_ShouldMapQuote_WhenLibraryReturnsPrice()
    {
        _twelveDataClient
            .Setup(x => x.GetRealTimePriceAsync("AAPL"))
            .ReturnsAsync(new TwelveDataPrice
            {
                Price = 177.12,
                ResponseStatus = Enums.TwelveDataClientResponseStatus.Ok
            });

        var sut = new TwelveDataMarketDataProvider(_configuration, _twelveDataClient.Object);

        var result = await sut.GetLatestQuoteAsync("aapl");

        result.ShouldNotBeNull();
        result.Symbol.ShouldBe("AAPL");
        result.Price.ShouldBe(177.12m);
        result.Source.ShouldBe("Twelve Data");
    }

    [Test]
    public async Task GetLatestQuoteAsync_ShouldThrowNotFound_WhenLibraryReturnsApiError()
    {
        _twelveDataClient
            .Setup(x => x.GetRealTimePriceAsync("MISSING"))
            .ReturnsAsync(new TwelveDataPrice
            {
                ResponseStatus = Enums.TwelveDataClientResponseStatus.TwelveDataApiError,
                ResponseMessage = "Invalid symbol or bad API key"
            });

        var sut = new TwelveDataMarketDataProvider(_configuration, _twelveDataClient.Object);

        var ex = await Should.ThrowAsync<MarketDataNotFoundException>(() => sut.GetLatestQuoteAsync("missing"));

        ex.Message.ShouldBe("Invalid symbol or bad API key");
    }

    [Test]
    public async Task GetLatestQuoteAsync_ShouldThrowDependencyException_WhenLibraryReturnsSharpError()
    {
        _twelveDataClient
            .Setup(x => x.GetRealTimePriceAsync("AAPL"))
            .ReturnsAsync(new TwelveDataPrice
            {
                ResponseStatus = Enums.TwelveDataClientResponseStatus.TwelveDataSharpError,
                ResponseMessage = "Socket failure"
            });

        var sut = new TwelveDataMarketDataProvider(_configuration, _twelveDataClient.Object);

        var ex = await Should.ThrowAsync<MarketDataDependencyException>(() => sut.GetLatestQuoteAsync("AAPL"));

        ex.Message.ShouldBe("Socket failure");
    }

    [Test]
    public async Task GetDetailedQuoteAsync_ShouldMapDetailedQuote_WhenLibraryReturnsQuote()
    {
        _twelveDataClient
            .Setup(x => x.GetQuoteAsync("AAPL", "1min"))
            .ReturnsAsync(new TwelveDataQuote
            {
                Symbol = "AAPL",
                Name = "Apple Inc.",
                Open = 100,
                High = 110,
                Low = 95,
                Close = 105,
                Volume = 1000,
                PreviousClose = 99,
                Change = 6,
                PercentChange = 6.06,
                ResponseStatus = Enums.TwelveDataClientResponseStatus.Ok
            });

        var sut = new TwelveDataMarketDataProvider(_configuration, _twelveDataClient.Object);

        var result = await sut.GetDetailedQuoteAsync("AAPL", "1min");

        result.ShouldNotBeNull();
        result.Symbol.ShouldBe("AAPL");
        result.Name.ShouldBe("Apple Inc.");
        result.Close.ShouldBe(105m);
    }
}
