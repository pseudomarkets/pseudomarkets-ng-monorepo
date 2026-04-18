using Moq;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.MarketData.Contracts.Quotes;
using PseudoMarkets.MarketData.Core.Exceptions;
using PseudoMarkets.MarketData.Core.Interfaces;
using PseudoMarkets.MarketData.Core.Services;

namespace PseudoMarkets.MarketData.Tests.Core;

[TestFixture]
public class QuoteServiceTests
{
    private Mock<IMarketDataCache> _marketDataCache = null!;
    private Mock<IMarketDataProvider> _marketDataProvider = null!;
    private QuoteService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _marketDataCache = new Mock<IMarketDataCache>();
        _marketDataProvider = new Mock<IMarketDataProvider>();
        _sut = new QuoteService(_marketDataCache.Object, _marketDataProvider.Object);
    }

    [Test]
    public async Task GetLatestQuoteAsync_ShouldThrow_WhenSymbolIsBlank()
    {
        var ex = await Should.ThrowAsync<MarketDataValidationException>(() => _sut.GetLatestQuoteAsync(" "));

        ex.Message.ShouldBe("A symbol is required.");
    }

    [Test]
    public async Task GetLatestQuoteAsync_ShouldReturnCachedQuote_WhenCacheHitOccurs()
    {
        var cachedQuote = new QuoteResponse
        {
            Symbol = "AAPL",
            Price = 123.45m,
            Source = "Aerospike",
            TimestampUtc = DateTimeOffset.UtcNow
        };

        _marketDataCache.Setup(x => x.GetLatestQuoteAsync("AAPL", It.IsAny<CancellationToken>())).ReturnsAsync(cachedQuote);

        var result = await _sut.GetLatestQuoteAsync("aapl");

        result.ShouldBe(cachedQuote);
        _marketDataProvider.Verify(x => x.GetLatestQuoteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetLatestQuoteAsync_ShouldFetchAndCacheProviderQuote_WhenCacheMissOccurs()
    {
        var providerQuote = new QuoteResponse
        {
            Symbol = "MSFT",
            Price = 456.78m,
            Source = "Twelve Data",
            TimestampUtc = DateTimeOffset.UtcNow
        };

        _marketDataCache.Setup(x => x.GetLatestQuoteAsync("MSFT", It.IsAny<CancellationToken>())).ReturnsAsync((QuoteResponse?)null);
        _marketDataProvider.Setup(x => x.GetLatestQuoteAsync("MSFT", It.IsAny<CancellationToken>())).ReturnsAsync(providerQuote);

        var result = await _sut.GetLatestQuoteAsync(" msft ");

        result.ShouldBe(providerQuote);
        _marketDataCache.Verify(x => x.SetLatestQuoteAsync(providerQuote, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetDetailedQuoteAsync_ShouldFetchAndCacheProviderDetailedQuote_WhenCacheMissOccurs()
    {
        var detailedQuote = new DetailedQuoteResponse
        {
            Symbol = "AAPL",
            Name = "Apple Inc.",
            Open = 100m,
            High = 101m,
            Low = 99m,
            Close = 100.5m,
            Volume = 1_000,
            PreviousClose = 99.5m,
            Change = 1m,
            ChangePercentage = 1.01m,
            Source = "Twelve Data",
            TimestampUtc = DateTimeOffset.UtcNow
        };

        _marketDataCache.Setup(x => x.GetDetailedQuoteAsync("AAPL", "1min", It.IsAny<CancellationToken>())).ReturnsAsync((DetailedQuoteResponse?)null);
        _marketDataProvider.Setup(x => x.GetDetailedQuoteAsync("AAPL", "1min", It.IsAny<CancellationToken>())).ReturnsAsync(detailedQuote);

        var result = await _sut.GetDetailedQuoteAsync("aapl", "1min");

        result.ShouldBe(detailedQuote);
        _marketDataCache.Verify(x => x.SetDetailedQuoteAsync(detailedQuote, "1min", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetUsMarketIndicesAsync_ShouldFetchAndCacheProviderIndices_WhenCacheMissOccurs()
    {
        var indices = new IndicesResponse
        {
            Indices =
            [
                new IndexSnapshotResponse { Name = "DOW", Points = 100m },
                new IndexSnapshotResponse { Name = "S&P 500", Points = 200m },
                new IndexSnapshotResponse { Name = "NASDAQ Composite", Points = 300m }
            ],
            Source = "Twelve Data Time Series",
            TimestampUtc = DateTimeOffset.UtcNow
        };

        _marketDataCache.Setup(x => x.GetUsMarketIndicesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IndicesResponse?)null);
        _marketDataProvider.Setup(x => x.GetUsMarketIndicesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(indices);

        var result = await _sut.GetUsMarketIndicesAsync();

        result.ShouldBe(indices);
        _marketDataCache.Verify(x => x.SetUsMarketIndicesAsync(indices, It.IsAny<CancellationToken>()), Times.Once);
    }
}
