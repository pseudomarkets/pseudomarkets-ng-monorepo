using Aerospike.Client;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.MarketData.Cache.Implementations;
using PseudoMarkets.MarketData.Contracts.Quotes;
using PseudoMarkets.MarketData.Core.Configuration;

namespace PseudoMarkets.MarketData.Tests.Cache;

[TestFixture]
public class AerospikeMarketDataCacheTests
{
    private Mock<IAerospikeClient> _aerospikeClient = null!;
    private AerospikeConfiguration _aerospikeConfiguration = null!;
    private MarketDataCacheConfiguration _cacheConfiguration = null!;
    private ILogger<AerospikeMarketDataCache> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _aerospikeClient = new Mock<IAerospikeClient>();
        _aerospikeConfiguration = new AerospikeConfiguration
        {
            Host = "localhost",
            Port = 3000
        };
        _cacheConfiguration = new MarketDataCacheConfiguration
        {
            QuoteTtlSeconds = 15
        };
        _logger = Mock.Of<ILogger<AerospikeMarketDataCache>>();
    }

    [Test]
    public async Task GetLatestQuoteAsync_ShouldReturnNull_WhenRecordDoesNotExist()
    {
        _aerospikeClient.Setup(x => x.Get(It.IsAny<Policy>(), It.IsAny<Key>())).Returns((Record)null!);
        var sut = CreateSut();

        var result = await sut.GetLatestQuoteAsync("AAPL");

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetLatestQuoteAsync_ShouldMapCachedRecord()
    {
        var timestampUtc = DateTimeOffset.UtcNow;
        var record = new Record(new Dictionary<string, object>
        {
            ["symbol"] = "AAPL",
            ["price"] = "123.45",
            ["source"] = "Aerospike",
            ["timestampUtc"] = timestampUtc.ToString("O")
        }, 0, 0);

        _aerospikeClient.Setup(x => x.Get(It.IsAny<Policy>(), It.IsAny<Key>())).Returns(record);
        var sut = CreateSut();

        var result = await sut.GetLatestQuoteAsync("AAPL");

        result.ShouldNotBeNull();
        result.Symbol.ShouldBe("AAPL");
        result.Price.ShouldBe(123.45m);
        result.Source.ShouldBe("Aerospike");
        result.TimestampUtc.ShouldBe(timestampUtc);
    }

    [Test]
    public async Task SetLatestQuoteAsync_ShouldWriteBinsToAerospike()
    {
        var sut = CreateSut();

        await sut.SetLatestQuoteAsync(new QuoteResponse
        {
            Symbol = "MSFT",
            Price = 456.78m,
            Source = "Twelve Data",
            TimestampUtc = DateTimeOffset.UtcNow
        });

        _aerospikeClient.Verify(x => x.Put(It.IsAny<WritePolicy>(), It.IsAny<Key>(), It.IsAny<Bin[]>()), Times.Once);
    }

    [Test]
    public async Task GetDetailedQuoteAsync_ShouldMapCachedRecord()
    {
        var timestampUtc = DateTimeOffset.UtcNow;
        var record = new Record(new Dictionary<string, object>
        {
            ["symbol"] = "AAPL",
            ["name"] = "Apple Inc.",
            ["open"] = "100",
            ["high"] = "101",
            ["low"] = "99",
            ["close"] = "100.5",
            ["volume"] = 1000L,
            ["previousClose"] = "99.5",
            ["change"] = "1",
            ["changePercentage"] = "1.01",
            ["source"] = "Aerospike",
            ["timestampUtc"] = timestampUtc.ToString("O")
        }, 0, 0);

        _aerospikeClient.Setup(x => x.Get(It.IsAny<Policy>(), It.IsAny<Key>())).Returns(record);
        var sut = CreateSut();

        var result = await sut.GetDetailedQuoteAsync("AAPL", "1min");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Apple Inc.");
        result.Close.ShouldBe(100.5m);
        result.TimestampUtc.ShouldBe(timestampUtc);
    }

    [Test]
    public async Task GetUsMarketIndicesAsync_ShouldDeserializeCachedPayload()
    {
        var response = new IndicesResponse
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

        var record = new Record(new Dictionary<string, object>
        {
            ["indicesPayload"] = System.Text.Json.JsonSerializer.Serialize(response)
        }, 0, 0);

        _aerospikeClient.Setup(x => x.Get(It.IsAny<Policy>(), It.IsAny<Key>())).Returns(record);
        var sut = CreateSut();

        var result = await sut.GetUsMarketIndicesAsync();

        result.ShouldNotBeNull();
        result.Indices.Count.ShouldBe(3);
        result.Source.ShouldBe("Twelve Data Time Series");
    }

    private AerospikeMarketDataCache CreateSut()
    {
        return new AerospikeMarketDataCache(_aerospikeClient.Object, _aerospikeConfiguration, _cacheConfiguration, _logger);
    }
}
