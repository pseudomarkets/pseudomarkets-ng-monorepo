using PseudoMarkets.OrderExecution.Core.Interfaces;
using PseudoMarkets.OrderExecution.Core.Models;

namespace PseudoMarkets.OrderExecution.Core.Services;

public sealed class MarketHoursEvaluator : IMarketHoursEvaluator
{
    private static readonly TimeSpan MarketOpenTime = new(9, 30, 0);
    private static readonly TimeSpan MarketCloseTime = new(16, 0, 0);
    private readonly IOrderExecutionRepository _repository;
    private readonly IClock _clock;
    private readonly TimeZoneInfo _newYorkTimeZone;

    public MarketHoursEvaluator(IOrderExecutionRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
        _newYorkTimeZone = ResolveNewYorkTimeZone();
    }

    public async Task<MarketHoursEvaluationResult> EvaluateAsync(CancellationToken cancellationToken)
    {
        var easternNow = TimeZoneInfo.ConvertTimeFromUtc(_clock.UtcNow, _newYorkTimeZone);

        if (easternNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return new MarketHoursEvaluationResult(false, "Weekend");
        }

        var localDate = DateOnly.FromDateTime(easternNow);
        if (await _repository.IsMarketHolidayAsync(localDate, cancellationToken))
        {
            return new MarketHoursEvaluationResult(false, "MarketHoliday");
        }

        var localTime = easternNow.TimeOfDay;
        if (localTime < MarketOpenTime)
        {
            return new MarketHoursEvaluationResult(false, "BeforeOpen");
        }

        if (localTime >= MarketCloseTime)
        {
            return new MarketHoursEvaluationResult(false, "AfterClose");
        }

        return new MarketHoursEvaluationResult(true, null);
    }

    private static TimeZoneInfo ResolveNewYorkTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
    }
}
