using Microsoft.EntityFrameworkCore;
using PseudoMarkets.OrderExecution.Core.Interfaces;
using PseudoMarkets.Shared.Entities.Database;
using PseudoMarkets.Shared.Entities.Entities.OrderExecution;
using PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;

namespace PseudoMarkets.OrderExecution.Persistence.Repositories;

public sealed class OrderExecutionRepository : IOrderExecutionRepository
{
    private readonly PseudoMarketsDbContext _dbContext;

    public OrderExecutionRepository(PseudoMarketsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> IsMarketHolidayAsync(DateOnly date, CancellationToken cancellationToken)
    {
        return _dbContext.MarketHolidays
            .AsNoTracking()
            .AnyAsync(x => x.HolidayDate == date, cancellationToken);
    }

    public Task<AccountBalanceEntity?> GetAccountBalanceAsync(long userId, CancellationToken cancellationToken)
    {
        return _dbContext.AccountBalances
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public Task<PositionEntity?> GetPositionAsync(long userId, string symbol, CancellationToken cancellationToken)
    {
        return _dbContext.Positions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Symbol == symbol, cancellationToken);
    }

    public async Task AddAsync(OrderExecutionEntity orderExecution, CancellationToken cancellationToken)
    {
        await _dbContext.OrderExecutions.AddAsync(orderExecution, cancellationToken);
    }

    public async Task AddQueuedOrderAsync(QueuedOrderEntity queuedOrder, CancellationToken cancellationToken)
    {
        await _dbContext.QueuedOrders.AddAsync(queuedOrder, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
