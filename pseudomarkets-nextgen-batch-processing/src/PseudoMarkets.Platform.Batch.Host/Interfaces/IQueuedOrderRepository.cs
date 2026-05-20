using PseudoMarkets.Shared.Entities.Entities.OrderExecution;

namespace PseudoMarkets.Platform.Batch.Host.Interfaces;

internal interface IQueuedOrderRepository
{
    Task<bool> IsMarketHolidayAsync(DateOnly date, CancellationToken cancellationToken);
    Task<List<QueuedOrderEntity>> GetPendingQueuedOrdersAsync(int maxBatchSize, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
