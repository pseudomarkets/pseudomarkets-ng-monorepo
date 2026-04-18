using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.MarketData.Contracts.Quotes;
using PseudoMarkets.MarketData.Core.Exceptions;
using PseudoMarkets.MarketData.Core.Interfaces;
using PseudoMarkets.MarketData.Service.Controllers;

namespace PseudoMarkets.MarketData.Tests.Service;

[TestFixture]
public class MarketDataControllerTests
{
    private Mock<IQuoteService> _quoteService = null!;
    private MarketDataController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _quoteService = new Mock<IQuoteService>();
        _sut = new MarketDataController(_quoteService.Object);
    }

    [Test]
    public async Task GetQuote_ShouldReturnOk_WhenQuoteExists()
    {
        var quote = new QuoteResponse
        {
            Symbol = "AAPL",
            Price = 123.45m,
            Source = "Twelve Data",
            TimestampUtc = DateTimeOffset.UtcNow
        };

        _quoteService.Setup(x => x.GetLatestQuoteAsync("AAPL", It.IsAny<CancellationToken>())).ReturnsAsync(quote);

        var result = await _sut.GetQuote("AAPL", CancellationToken.None);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(quote);
    }

    [Test]
    public async Task GetQuote_ShouldReturnBadRequest_WhenValidationFails()
    {
        _quoteService
            .Setup(x => x.GetLatestQuoteAsync(" ", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MarketDataValidationException("A symbol is required."));

        var result = await _sut.GetQuote(" ", CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task GetQuote_ShouldReturnNotFound_WhenProviderCannotFindSymbol()
    {
        _quoteService
            .Setup(x => x.GetLatestQuoteAsync("MISSING", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MarketDataNotFoundException("No quote was found."));

        var result = await _sut.GetQuote("MISSING", CancellationToken.None);

        result.Result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetDetailedQuote_ShouldReturnOk_WhenDetailedQuoteExists()
    {
        var quote = new DetailedQuoteResponse
        {
            Symbol = "AAPL",
            Name = "Apple Inc.",
            Open = 100m,
            High = 101m,
            Low = 99m,
            Close = 100.5m,
            Volume = 1000,
            PreviousClose = 99.5m,
            Change = 1m,
            ChangePercentage = 1.01m,
            Source = "Twelve Data",
            TimestampUtc = DateTimeOffset.UtcNow
        };

        _quoteService.Setup(x => x.GetDetailedQuoteAsync("AAPL", "1min", It.IsAny<CancellationToken>())).ReturnsAsync(quote);

        var result = await _sut.GetDetailedQuote("AAPL", "1min", CancellationToken.None);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(quote);
    }

    [Test]
    public async Task GetIndices_ShouldReturnOk_WhenIndicesExist()
    {
        var indices = new IndicesResponse
        {
            Indices =
            [
                new IndexSnapshotResponse { Name = "DOW", Points = 100m }
            ],
            Source = "Twelve Data Time Series",
            TimestampUtc = DateTimeOffset.UtcNow
        };

        _quoteService.Setup(x => x.GetUsMarketIndicesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(indices);

        var result = await _sut.GetIndices(CancellationToken.None);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(indices);
    }
}
