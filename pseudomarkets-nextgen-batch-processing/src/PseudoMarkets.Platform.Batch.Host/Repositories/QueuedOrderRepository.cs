using Microsoft.EntityFrameworkCore;
using PseudoMarkets.Platform.Batch.Host.Constants;
using PseudoMarkets.Platform.Batch.Host.Interfaces;
using PseudoMarkets.Shared.Entities.Database;
using PseudoMarkets.Shared.Entities.Entities.OrderExecution;

namespace PseudoMarkets.Platform.Batch.Host.Repositories;

internal sealed class QueuedOrderRepository : IQueuedOrderRepository
{
    private readonly PseudoMarketsDbContext _dbContext;

    public QueuedOrderRepository(PseudoMarketsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> IsMarketHolidayAsync(DateOnly date, CancellationToken cancellationToken)
    {
        return _dbContext.MarketHolidays
            .AsNoTracking()
            .AnyAsync(x => x.HolidayDate == date, cancellationToken);
    }

    public Task<List<QueuedOrderEntity>> GetPendingQueuedOrdersAsync(int maxBatchSize, CancellationToken cancellationToken)
    {
        var effectiveBatchSize = maxBatchSize > 0 ? maxBatchSize : 1000;

        return _dbContext.QueuedOrders
            .Where(x => x.Status == QueuedOrderExecutionConstants.PendingStatus)
            .OrderBy(x => x.SubmittedAtUtc)
            .ThenBy(x => x.OrderId)
            .Take(effectiveBatchSize)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
