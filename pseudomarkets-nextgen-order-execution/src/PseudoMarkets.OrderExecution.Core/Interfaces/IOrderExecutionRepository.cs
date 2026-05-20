using PseudoMarkets.Shared.Entities.Entities.OrderExecution;
using PseudoMarkets.Shared.Entities.Entities.Platform;
using PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;

namespace PseudoMarkets.OrderExecution.Core.Interfaces;

public interface IOrderExecutionRepository
{
    Task<bool> IsMarketHolidayAsync(DateOnly date, CancellationToken cancellationToken);
    Task<AccountBalanceEntity?> GetAccountBalanceAsync(long userId, CancellationToken cancellationToken);
    Task<PositionEntity?> GetPositionAsync(long userId, string symbol, CancellationToken cancellationToken);
    Task AddAsync(OrderExecutionEntity orderExecution, CancellationToken cancellationToken);
    Task AddQueuedOrderAsync(QueuedOrderEntity queuedOrder, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
