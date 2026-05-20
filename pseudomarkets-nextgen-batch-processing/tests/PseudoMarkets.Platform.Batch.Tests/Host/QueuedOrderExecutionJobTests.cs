using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PseudoMarkets.OrderExecution.Contracts.Enums;
using PseudoMarkets.OrderExecution.Contracts.Orders;
using PseudoMarkets.Platform.Batch.Host.Configuration;
using PseudoMarkets.Platform.Batch.Host.Constants;
using PseudoMarkets.Platform.Batch.Host.Interfaces;
using PseudoMarkets.Platform.Batch.Host.Jobs;
using PseudoMarkets.Shared.Entities.Entities.OrderExecution;
using Shouldly;

namespace PseudoMarkets.Platform.Batch.Tests.Host;

[TestFixture]
public sealed class QueuedOrderExecutionJobTests
{
    [Test]
    public async Task ExecuteAsync_ShouldSkipProcessing_WhenMarketIsClosed()
    {
        var repository = new FakeQueuedOrderRepository();
        var marketOpenEvaluator = new FakeMarketOpenEvaluator(false);
        var orderExecutionClient = new FakeOrderExecutionClient();
        var sut = CreateSut(repository, marketOpenEvaluator, orderExecutionClient);

        await sut.ExecuteAsync(CancellationToken.None);

        repository.GetPendingQueuedOrdersCallCount.ShouldBe(0);
        orderExecutionClient.Submissions.ShouldBeEmpty();
    }

    [Test]
    public async Task ExecuteAsync_ShouldProcessPendingOrdersUsingConfiguredBatchSize()
    {
        var queuedOrder = CreateQueuedOrder(userId: 1000000001);
        var repository = new FakeQueuedOrderRepository(queuedOrder);
        var marketOpenEvaluator = new FakeMarketOpenEvaluator(true);
        var orderExecutionClient = new FakeOrderExecutionClient(new SubmitOrderResponse
        {
            Disposition = OrderDisposition.Executed,
            OrderId = Guid.NewGuid(),
            ExecutionId = Guid.NewGuid(),
            TransactionId = Guid.NewGuid(),
            PostingBatchId = Guid.NewGuid(),
            UserId = queuedOrder.UserId,
            Symbol = queuedOrder.Symbol,
            Side = OrderSide.Buy,
            OrderType = OrderType.Market,
            Quantity = queuedOrder.Quantity,
            FillPrice = 100m,
            GrossAmount = 100m,
            Fees = 0m,
            NetAmount = 100m,
            Status = OrderStatus.Filled,
            SubmittedAtUtc = queuedOrder.SubmittedAtUtc,
            ExecutedAtUtc = queuedOrder.SubmittedAtUtc.AddMinutes(1)
        });
        var sut = CreateSut(repository, marketOpenEvaluator, orderExecutionClient, maxBatchSize: 17);

        await sut.ExecuteAsync(CancellationToken.None);

        repository.RequestedBatchSize.ShouldBe(17);
        orderExecutionClient.Submissions.Count.ShouldBe(1);
        orderExecutionClient.Submissions.Single().UserId.ShouldBe(queuedOrder.UserId);
        queuedOrder.Status.ShouldBe(QueuedOrderExecutionConstants.SucceededStatus);
        queuedOrder.ProcessedAtUtc.ShouldNotBeNull();
        queuedOrder.FailureMessage.ShouldBeNull();
    }

    [Test]
    public async Task ExecuteAsync_ShouldMarkOrderFailed_WhenOrderExecutionReturnsQueuedDisposition()
    {
        var queuedOrder = CreateQueuedOrder(userId: 1000000002);
        var repository = new FakeQueuedOrderRepository(queuedOrder);
        var marketOpenEvaluator = new FakeMarketOpenEvaluator(true);
        var orderExecutionClient = new FakeOrderExecutionClient(new SubmitOrderResponse
        {
            Disposition = OrderDisposition.Queued,
            OrderId = Guid.NewGuid(),
            UserId = queuedOrder.UserId,
            Symbol = queuedOrder.Symbol,
            Side = OrderSide.Buy,
            OrderType = OrderType.Market,
            Quantity = queuedOrder.Quantity,
            Status = OrderStatus.Queued,
            SubmittedAtUtc = queuedOrder.SubmittedAtUtc
        });
        var sut = CreateSut(repository, marketOpenEvaluator, orderExecutionClient);

        await sut.ExecuteAsync(CancellationToken.None);

        queuedOrder.Status.ShouldBe(QueuedOrderExecutionConstants.FailedStatus);
        queuedOrder.ProcessedAtUtc.ShouldNotBeNull();
        queuedOrder.FailureMessage.ShouldNotBeNull();
        queuedOrder.FailureMessage.ShouldContain("disposition");
    }

    [Test]
    public async Task ExecuteAsync_ShouldMarkOrderFailed_WhenOrderExecutionThrows()
    {
        var queuedOrder = CreateQueuedOrder(userId: 1000000003);
        var repository = new FakeQueuedOrderRepository(queuedOrder);
        var marketOpenEvaluator = new FakeMarketOpenEvaluator(true);
        var orderExecutionClient = new FakeOrderExecutionClient(new InvalidOperationException("downstream failure"));
        var sut = CreateSut(repository, marketOpenEvaluator, orderExecutionClient);

        await sut.ExecuteAsync(CancellationToken.None);

        queuedOrder.Status.ShouldBe(QueuedOrderExecutionConstants.FailedStatus);
        queuedOrder.ProcessedAtUtc.ShouldNotBeNull();
        queuedOrder.FailureMessage.ShouldBe("downstream failure");
    }

    private static QueuedOrderExecutionJob CreateSut(
        FakeQueuedOrderRepository repository,
        FakeMarketOpenEvaluator marketOpenEvaluator,
        FakeOrderExecutionClient orderExecutionClient,
        int maxBatchSize = 1000)
    {
        var clock = new FixedClock(new DateTime(2026, 05, 18, 14, 30, 00, DateTimeKind.Utc));
        return new QueuedOrderExecutionJob(
            repository,
            marketOpenEvaluator,
            orderExecutionClient,
            clock,
            Options.Create(new QueuedOrderExecutionConfiguration { MaxBatchSize = maxBatchSize }),
            NullLogger<QueuedOrderExecutionJob>.Instance);
    }

    private static QueuedOrderEntity CreateQueuedOrder(long userId)
    {
        return new QueuedOrderEntity
        {
            OrderId = Guid.NewGuid(),
            UserId = userId,
            Symbol = "AAPL",
            OrderSide = "Buy",
            OrderType = "Market",
            Quantity = 1,
            Status = QueuedOrderExecutionConstants.PendingStatus,
            QueueReason = "AfterClose",
            SubmittedAtUtc = new DateTime(2026, 05, 18, 13, 00, 00, DateTimeKind.Utc),
            CreatedAtUtc = new DateTime(2026, 05, 18, 13, 00, 00, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 05, 18, 13, 00, 00, DateTimeKind.Utc)
        };
    }

    private sealed class FakeQueuedOrderRepository : IQueuedOrderRepository
    {
        private readonly List<QueuedOrderEntity> _queuedOrders;

        public FakeQueuedOrderRepository(params QueuedOrderEntity[] queuedOrders)
        {
            _queuedOrders = queuedOrders.ToList();
        }

        public int RequestedBatchSize { get; private set; }
        public int GetPendingQueuedOrdersCallCount { get; private set; }

        public Task<bool> IsMarketHolidayAsync(DateOnly date, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<List<QueuedOrderEntity>> GetPendingQueuedOrdersAsync(int maxBatchSize, CancellationToken cancellationToken)
        {
            RequestedBatchSize = maxBatchSize;
            GetPendingQueuedOrdersCallCount++;
            return Task.FromResult(_queuedOrders);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMarketOpenEvaluator : IMarketOpenEvaluator
    {
        private readonly bool _isMarketOpen;

        public FakeMarketOpenEvaluator(bool isMarketOpen)
        {
            _isMarketOpen = isMarketOpen;
        }

        public Task<bool> IsMarketOpenAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_isMarketOpen);
        }
    }

    private sealed class FakeOrderExecutionClient : IOrderExecutionClient
    {
        private readonly SubmitOrderResponse? _response;
        private readonly Exception? _exception;

        public FakeOrderExecutionClient(SubmitOrderResponse? response = null)
        {
            _response = response;
        }

        public FakeOrderExecutionClient(Exception exception)
        {
            _exception = exception;
        }

        public List<QueuedOrderEntity> Submissions { get; } = [];

        public Task<SubmitOrderResponse> SubmitQueuedOrderAsync(QueuedOrderEntity queuedOrder, CancellationToken cancellationToken)
        {
            Submissions.Add(queuedOrder);
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_response!);
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
