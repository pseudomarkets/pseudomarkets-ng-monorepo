using PseudoMarkets.Shared.Entities.Entities.OrderExecution;
using PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;

namespace PseudoMarkets.OrderExecution.Core.Interfaces;

public interface IOrderExecutionRepository
{
    Task<AccountBalanceEntity?> GetAccountBalanceAsync(long userId, CancellationToken cancellationToken);
    Task<PositionEntity?> GetPositionAsync(long userId, string symbol, CancellationToken cancellationToken);
    Task AddAsync(OrderExecutionEntity orderExecution, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
