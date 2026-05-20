using Microsoft.EntityFrameworkCore;
using PseudoMarkets.Platform.Batch.Host.Constants;
using PseudoMarkets.Platform.Batch.Host.Repositories;
using PseudoMarkets.Shared.Entities.Database;
using PseudoMarkets.Shared.Entities.Entities.OrderExecution;
using Shouldly;

namespace PseudoMarkets.Platform.Batch.Tests.Host;

[TestFixture]
public sealed class QueuedOrderRepositoryTests
{
    [Test]
    public async Task GetPendingQueuedOrdersAsync_ShouldReturnPendingOrdersInSubmittedOrderAndRespectBatchSize()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.QueuedOrders.AddRangeAsync(
            new QueuedOrderEntity
            {
                OrderId = Guid.NewGuid(),
                UserId = 1000000003,
                Symbol = "MSFT",
                OrderSide = "Buy",
                OrderType = "Market",
                Quantity = 1,
                Status = QueuedOrderExecutionConstants.PendingStatus,
                QueueReason = "AfterClose",
                SubmittedAtUtc = new DateTime(2026, 05, 18, 14, 00, 00, DateTimeKind.Utc),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new QueuedOrderEntity
            {
                OrderId = Guid.NewGuid(),
                UserId = 1000000001,
                Symbol = "AAPL",
                OrderSide = "Buy",
                OrderType = "Market",
                Quantity = 1,
                Status = QueuedOrderExecutionConstants.PendingStatus,
                QueueReason = "AfterClose",
                SubmittedAtUtc = new DateTime(2026, 05, 18, 12, 00, 00, DateTimeKind.Utc),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new QueuedOrderEntity
            {
                OrderId = Guid.NewGuid(),
                UserId = 1000000002,
                Symbol = "GOOG",
                OrderSide = "Buy",
                OrderType = "Market",
                Quantity = 1,
                Status = QueuedOrderExecutionConstants.FailedStatus,
                QueueReason = "AfterClose",
                SubmittedAtUtc = new DateTime(2026, 05, 18, 13, 00, 00, DateTimeKind.Utc),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        await dbContext.SaveChangesAsync();

        var sut = new QueuedOrderRepository(dbContext);

        var queuedOrders = await sut.GetPendingQueuedOrdersAsync(1, CancellationToken.None);

        queuedOrders.Count.ShouldBe(1);
        queuedOrders.Single().UserId.ShouldBe(1000000001);
    }

    private static PseudoMarketsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PseudoMarketsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PseudoMarketsDbContext(options);
    }
}
