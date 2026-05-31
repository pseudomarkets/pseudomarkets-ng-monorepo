using NUnit.Framework;
using PseudoMarkets.BalancesAndPositions.Contracts.Enums;
using PseudoMarkets.BalancesAndPositions.Contracts.Requests;
using PseudoMarkets.BalancesAndPositions.Core.Exceptions;
using PseudoMarkets.BalancesAndPositions.Core.Services;
using PseudoMarkets.BalancesAndPositions.Tests.Support;
using PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;
using Shouldly;

namespace PseudoMarkets.BalancesAndPositions.Tests.Core;

[TestFixture]
public sealed class BalanceQueryServiceTests : BalancesAndPositionsTestBase
{
    [Test]
    public async Task GetBalanceAsync_ShouldReturnAllBalanceFields_WhenViewIsAll()
    {
        DbContext.AccountBalances.Add(new AccountBalanceEntity
        {
            UserId = 1_000_000_001,
            CashBalance = 150m,
            SettledCashBalance = 100m,
            UnsettledCashBalance = 50m
        });
        await DbContext.SaveChangesAsync();

        var sut = new BalanceQueryService(DbContext);

        var response = await sut.GetBalanceAsync(
            new BalanceQueryRequest { UserId = 1_000_000_001, View = PositionView.All },
            CancellationToken.None);

        response.AggregateCashBalance.ShouldBe(150m);
        response.SettledCashBalance.ShouldBe(100m);
        response.UnsettledCashBalance.ShouldBe(50m);
    }

    [Test]
    public async Task GetBalanceAsync_ShouldFilterToSettledFields_WhenViewIsSettled()
    {
        DbContext.AccountBalances.Add(new AccountBalanceEntity
        {
            UserId = 1_000_000_001,
            CashBalance = 150m,
            SettledCashBalance = 100m,
            UnsettledCashBalance = 50m
        });
        await DbContext.SaveChangesAsync();

        var sut = new BalanceQueryService(DbContext);

        var response = await sut.GetBalanceAsync(
            new BalanceQueryRequest { UserId = 1_000_000_001, View = PositionView.Settled },
            CancellationToken.None);

        response.AggregateCashBalance.ShouldBeNull();
        response.SettledCashBalance.ShouldBe(100m);
        response.UnsettledCashBalance.ShouldBeNull();
    }

    [Test]
    public async Task GetBalanceAsync_ShouldFilterToUnsettledFields_WhenViewIsUnsettled()
    {
        DbContext.AccountBalances.Add(new AccountBalanceEntity
        {
            UserId = 1_000_000_001,
            CashBalance = 150m,
            SettledCashBalance = 100m,
            UnsettledCashBalance = 50m
        });
        await DbContext.SaveChangesAsync();

        var sut = new BalanceQueryService(DbContext);

        var response = await sut.GetBalanceAsync(
            new BalanceQueryRequest { UserId = 1_000_000_001, View = PositionView.Unsettled },
            CancellationToken.None);

        response.AggregateCashBalance.ShouldBeNull();
        response.SettledCashBalance.ShouldBeNull();
        response.UnsettledCashBalance.ShouldBe(50m);
    }

    [Test]
    public void GetBalanceAsync_ShouldThrowNotFound_WhenBalanceRecordDoesNotExist()
    {
        var sut = new BalanceQueryService(DbContext);

        Should.ThrowAsync<BalancesAndPositionsNotFoundException>(() => sut.GetBalanceAsync(
            new BalanceQueryRequest { UserId = 1_000_000_001 },
            CancellationToken.None));
    }
}
