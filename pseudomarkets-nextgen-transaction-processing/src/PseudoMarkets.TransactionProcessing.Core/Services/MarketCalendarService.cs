using Microsoft.EntityFrameworkCore;
using PseudoMarkets.Shared.Entities.Database;
using PseudoMarkets.TransactionProcessing.Core.Interfaces;

namespace PseudoMarkets.TransactionProcessing.Core.Services;

public class MarketCalendarService : IMarketCalendarService
{
    private static readonly TimeZoneInfo EasternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private readonly PseudoMarketsDbContext _dbContext;

    public MarketCalendarService(PseudoMarketsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DateOnly GetTradeDate(DateTime executedAtUtc)
    {
        var normalizedExecutedAtUtc = executedAtUtc.Kind == DateTimeKind.Utc
            ? executedAtUtc
            : DateTime.SpecifyKind(executedAtUtc, DateTimeKind.Utc);

        var easternExecutedAt = TimeZoneInfo.ConvertTimeFromUtc(normalizedExecutedAtUtc, EasternTimeZone);
        return DateOnly.FromDateTime(easternExecutedAt);
    }

    public async Task<DateOnly> GetSettlementDateAsync(DateOnly tradeDate, CancellationToken cancellationToken = default)
    {
        var candidate = tradeDate.AddDays(1);

        while (!await IsMarketBusinessDayAsync(candidate, cancellationToken))
        {
            candidate = candidate.AddDays(1);
        }

        return candidate;
    }

    public async Task<bool> IsMarketBusinessDayAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        return !await _dbContext.MarketHolidays
            .AsNoTracking()
            .AnyAsync(x => x.HolidayDate == date, cancellationToken);
    }
}
