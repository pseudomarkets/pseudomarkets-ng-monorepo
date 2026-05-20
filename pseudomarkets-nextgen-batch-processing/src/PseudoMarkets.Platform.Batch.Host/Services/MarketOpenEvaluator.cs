using PseudoMarkets.Platform.Batch.Host.Interfaces;
using PseudoMarkets.Platform.Batch.Host.Jobs;

namespace PseudoMarkets.Platform.Batch.Host.Services;

internal sealed class MarketOpenEvaluator : IMarketOpenEvaluator
{
    private static readonly TimeOnly MarketOpenTime = new(9, 30);
    private static readonly TimeOnly MarketCloseTime = new(16, 0);
    private readonly IQueuedOrderRepository _queuedOrderRepository;
    private readonly IClock _clock;

    public MarketOpenEvaluator(IQueuedOrderRepository queuedOrderRepository, IClock clock)
    {
        _queuedOrderRepository = queuedOrderRepository;
        _clock = clock;
    }

    public async Task<bool> IsMarketOpenAsync(CancellationToken cancellationToken)
    {
        var easternNow = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(_clock.UtcNow, QueuedOrderExecutionJob.TimeZoneId);
        if (easternNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        if (await _queuedOrderRepository.IsMarketHolidayAsync(DateOnly.FromDateTime(easternNow), cancellationToken))
        {
            return false;
        }

        var currentTime = TimeOnly.FromDateTime(easternNow);
        return currentTime >= MarketOpenTime && currentTime < MarketCloseTime;
    }
}
