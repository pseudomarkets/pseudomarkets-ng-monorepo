using NUnit.Framework;
using Shouldly;
using PseudoMarkets.TransactionProcessing.Tests.Support;

namespace PseudoMarkets.TransactionProcessing.Tests.Core;

[TestFixture]
public class MarketCalendarServiceTests : TransactionProcessingTestBase
{
    [Test]
    public void GetTradeDate_ShouldUseEasternTime()
    {
        var tradeDate = MarketCalendarService.GetTradeDate(new DateTime(2026, 1, 16, 2, 30, 0, DateTimeKind.Utc));

        tradeDate.ShouldBe(new DateOnly(2026, 1, 15));
    }

    [Test]
    public async Task GetSettlementDateAsync_ShouldSettleNextMarketBusinessDay()
    {
        var settlementDate = await MarketCalendarService.GetSettlementDateAsync(new DateOnly(2026, 1, 14));

        settlementDate.ShouldBe(new DateOnly(2026, 1, 15));
    }

    [Test]
    public async Task GetSettlementDateAsync_ShouldSkipWeekend()
    {
        var settlementDate = await MarketCalendarService.GetSettlementDateAsync(new DateOnly(2026, 1, 16));

        settlementDate.ShouldBe(new DateOnly(2026, 1, 20));
    }

    [Test]
    public async Task GetSettlementDateAsync_ShouldSkipMarketHoliday()
    {
        var settlementDate = await MarketCalendarService.GetSettlementDateAsync(new DateOnly(2026, 1, 18));

        settlementDate.ShouldBe(new DateOnly(2026, 1, 20));
    }

    [Test]
    public async Task GetSettlementDateAsync_ShouldSkipHolidayAndWeekend()
    {
        var settlementDate = await MarketCalendarService.GetSettlementDateAsync(new DateOnly(2026, 7, 2));

        settlementDate.ShouldBe(new DateOnly(2026, 7, 6));
    }
}
