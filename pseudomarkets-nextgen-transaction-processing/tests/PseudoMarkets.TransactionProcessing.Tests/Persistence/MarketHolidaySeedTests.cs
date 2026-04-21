using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PseudoMarkets.TransactionProcessing.Tests.Support;
using Shouldly;

namespace PseudoMarkets.TransactionProcessing.Tests.Persistence;

[TestFixture]
public class MarketHolidaySeedTests : TransactionProcessingTestBase
{
    [Test]
    public async Task SharedModel_ShouldSeed2026NyseMarketHolidays()
    {
        var holidays = await DbContext.MarketHolidays
            .OrderBy(x => x.HolidayDate)
            .ToListAsync();

        holidays.Count.ShouldBe(10);
        holidays.Select(x => x.HolidayDate).ShouldBe(
            [
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 19),
                new DateOnly(2026, 2, 16),
                new DateOnly(2026, 4, 3),
                new DateOnly(2026, 5, 25),
                new DateOnly(2026, 6, 19),
                new DateOnly(2026, 7, 3),
                new DateOnly(2026, 9, 7),
                new DateOnly(2026, 11, 26),
                new DateOnly(2026, 12, 25)
            ]);
        holidays.Single(x => x.HolidayDate == new DateOnly(2026, 4, 3)).HolidayName.ShouldBe("Good Friday");
    }
}
