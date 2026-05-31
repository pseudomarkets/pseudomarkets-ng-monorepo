using Moq;
using NUnit.Framework;
using PseudoMarkets.BalancesAndPositions.Contracts.Enums;
using PseudoMarkets.BalancesAndPositions.Contracts.Requests;
using PseudoMarkets.BalancesAndPositions.Core.Interfaces;
using PseudoMarkets.BalancesAndPositions.Core.Models;
using PseudoMarkets.BalancesAndPositions.Core.Services;
using PseudoMarkets.BalancesAndPositions.Tests.Support;
using PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;
using Shouldly;

namespace PseudoMarkets.BalancesAndPositions.Tests.Core;

[TestFixture]
public sealed class PositionQueryServiceTests : BalancesAndPositionsTestBase
{
    private Mock<IMarketDataQuoteClient> _marketDataQuoteClient = null!;

    [SetUp]
    public void TestSetUp()
    {
        _marketDataQuoteClient = new Mock<IMarketDataQuoteClient>();
    }

    [Test]
    public async Task GetPositionsAsync_ShouldReturnEmptyCollection_WhenUserHasNoPositions()
    {
        var sut = new PositionQueryService(DbContext, _marketDataQuoteClient.Object);

        var response = await sut.GetPositionsAsync(
            new PositionQueryRequest { UserId = 1_000_000_001, View = PositionView.All },
            CancellationToken.None);

        response.Positions.ShouldBeEmpty();
        response.Warnings.ShouldBeEmpty();
    }

    [Test]
    public async Task GetPositionsAsync_ShouldExcludeZeroQuantityPositions()
    {
        DbContext.Positions.AddRange(
            new PositionEntity
            {
                UserId = 1_000_000_001,
                Symbol = "AAPL",
                PositionSide = "LONG",
                Quantity = 0m,
                SettledQuantity = 0m,
                UnsettledQuantity = 0m,
                CostBasisTotal = 0m,
                SettledCostBasisTotal = 0m,
                UnsettledCostBasisTotal = 0m
            },
            new PositionEntity
            {
                UserId = 1_000_000_001,
                Symbol = "MSFT",
                PositionSide = "LONG",
                Quantity = 10m,
                SettledQuantity = 7m,
                UnsettledQuantity = 3m,
                CostBasisTotal = 900m,
                SettledCostBasisTotal = 630m,
                UnsettledCostBasisTotal = 270m
            });
        await DbContext.SaveChangesAsync();

        _marketDataQuoteClient
            .Setup(client => client.GetQuoteAsync("MSFT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuoteLookupResult(true, 100m, null, null));

        var sut = new PositionQueryService(DbContext, _marketDataQuoteClient.Object);

        var response = await sut.GetPositionsAsync(
            new PositionQueryRequest { UserId = 1_000_000_001, View = PositionView.All },
            CancellationToken.None);

        response.Positions.Count.ShouldBe(1);
        response.Positions.Single().Symbol.ShouldBe("MSFT");
    }

    [Test]
    public async Task GetPositionsAsync_ShouldCalculateMarketValueAndUnrealizedGainLoss_WhenQuoteExists()
    {
        DbContext.Positions.Add(new PositionEntity
        {
            UserId = 1_000_000_001,
            Symbol = "AAPL",
            PositionSide = "LONG",
            Quantity = 10m,
            SettledQuantity = 8m,
            UnsettledQuantity = 2m,
            CostBasisTotal = 1_000m,
            SettledCostBasisTotal = 800m,
            UnsettledCostBasisTotal = 200m
        });
        await DbContext.SaveChangesAsync();

        _marketDataQuoteClient
            .Setup(client => client.GetQuoteAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuoteLookupResult(true, 125m, null, null));

        var sut = new PositionQueryService(DbContext, _marketDataQuoteClient.Object);

        var response = await sut.GetPositionsAsync(
            new PositionQueryRequest { UserId = 1_000_000_001, View = PositionView.All },
            CancellationToken.None);

        var position = response.Positions.Single();
        position.CurrentMarketPrice.ShouldBe(125m);
        position.AggregateMarketValue.ShouldBe(1_250m);
        position.SettledMarketValue.ShouldBe(1_000m);
        position.UnsettledMarketValue.ShouldBe(250m);
        position.AggregateUnrealizedGainLoss.ShouldBe(250m);
        position.SettledUnrealizedGainLoss.ShouldBe(200m);
        position.UnsettledUnrealizedGainLoss.ShouldBe(50m);
        position.IsQuoteAvailable.ShouldBeTrue();
        response.Warnings.ShouldBeEmpty();
    }

    [Test]
    public async Task GetPositionsAsync_ShouldApplySettledViewFiltering()
    {
        DbContext.Positions.Add(new PositionEntity
        {
            UserId = 1_000_000_001,
            Symbol = "AAPL",
            PositionSide = "LONG",
            Quantity = 10m,
            SettledQuantity = 8m,
            UnsettledQuantity = 2m,
            CostBasisTotal = 1_000m,
            SettledCostBasisTotal = 800m,
            UnsettledCostBasisTotal = 200m
        });
        await DbContext.SaveChangesAsync();

        _marketDataQuoteClient
            .Setup(client => client.GetQuoteAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuoteLookupResult(true, 125m, null, null));

        var sut = new PositionQueryService(DbContext, _marketDataQuoteClient.Object);

        var response = await sut.GetPositionsAsync(
            new PositionQueryRequest { UserId = 1_000_000_001, View = PositionView.Settled },
            CancellationToken.None);

        var position = response.Positions.Single();
        position.AggregateQuantity.ShouldBeNull();
        position.SettledQuantity.ShouldBe(8m);
        position.UnsettledQuantity.ShouldBeNull();
        position.AggregateMarketValue.ShouldBeNull();
        position.SettledMarketValue.ShouldBe(1_000m);
        position.UnsettledMarketValue.ShouldBeNull();
    }

    [Test]
    public async Task GetPositionsAsync_ShouldReturnWarningsAndNullQuoteDerivedFields_WhenQuoteIsUnavailable()
    {
        DbContext.Positions.Add(new PositionEntity
        {
            UserId = 1_000_000_001,
            Symbol = "AAPL",
            PositionSide = "LONG",
            Quantity = 10m,
            SettledQuantity = 8m,
            UnsettledQuantity = 2m,
            CostBasisTotal = 1_000m,
            SettledCostBasisTotal = 800m,
            UnsettledCostBasisTotal = 200m
        });
        await DbContext.SaveChangesAsync();

        _marketDataQuoteClient
            .Setup(client => client.GetQuoteAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuoteLookupResult(false, null, "QUOTE_UNAVAILABLE", "Quote failed."));

        var sut = new PositionQueryService(DbContext, _marketDataQuoteClient.Object);

        var response = await sut.GetPositionsAsync(
            new PositionQueryRequest { UserId = 1_000_000_001, View = PositionView.All },
            CancellationToken.None);

        var position = response.Positions.Single();
        position.IsQuoteAvailable.ShouldBeFalse();
        position.CurrentMarketPrice.ShouldBeNull();
        position.AggregateMarketValue.ShouldBeNull();
        position.AggregateUnrealizedGainLoss.ShouldBeNull();
        response.Warnings.Count.ShouldBe(1);
        response.Warnings.Single().Symbol.ShouldBe("AAPL");
    }
}
