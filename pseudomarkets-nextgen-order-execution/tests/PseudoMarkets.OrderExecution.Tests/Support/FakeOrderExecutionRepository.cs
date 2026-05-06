using PseudoMarkets.OrderExecution.Core.Interfaces;
using PseudoMarkets.Shared.Entities.Entities.OrderExecution;
using PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;

namespace PseudoMarkets.OrderExecution.Tests.Support;

internal sealed class FakeOrderExecutionRepository : IOrderExecutionRepository
{
    public AccountBalanceEntity? AccountBalance { get; set; }
    public PositionEntity? Position { get; set; }
    public List<OrderExecutionEntity> OrderExecutions { get; } = [];
    public int SaveChangesCount { get; private set; }

    public Task<AccountBalanceEntity?> GetAccountBalanceAsync(long userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(AccountBalance?.UserId == userId ? AccountBalance : null);
    }

    public Task<PositionEntity?> GetPositionAsync(long userId, string symbol, CancellationToken cancellationToken)
    {
        return Task.FromResult(Position?.UserId == userId && Position.Symbol == symbol ? Position : null);
    }

    public Task AddAsync(OrderExecutionEntity orderExecution, CancellationToken cancellationToken)
    {
        OrderExecutions.Add(orderExecution);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCount++;
        return Task.CompletedTask;
    }
}
