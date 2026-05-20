using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PseudoMarkets.OrderExecution.Contracts.Enums;
using PseudoMarkets.Platform.Batch.Core.Interfaces;
using PseudoMarkets.Platform.Batch.Host.Configuration;
using PseudoMarkets.Platform.Batch.Host.Constants;
using PseudoMarkets.Platform.Batch.Host.Interfaces;
using PseudoMarkets.Shared.Entities.Entities.OrderExecution;

namespace PseudoMarkets.Platform.Batch.Host.Jobs;

internal sealed class QueuedOrderExecutionJob : IBatchJob
{
    public const string JobName = "queued-order-execution";
    public const string DefaultCronExpression = "30 9 * * 1-5";
    public const string TimeZoneId = "America/New_York";

    private readonly IQueuedOrderRepository _queuedOrderRepository;
    private readonly IMarketOpenEvaluator _marketOpenEvaluator;
    private readonly IOrderExecutionClient _orderExecutionClient;
    private readonly IClock _clock;
    private readonly QueuedOrderExecutionConfiguration _configuration;
    private readonly ILogger<QueuedOrderExecutionJob> _logger;

    public QueuedOrderExecutionJob(
        IQueuedOrderRepository queuedOrderRepository,
        IMarketOpenEvaluator marketOpenEvaluator,
        IOrderExecutionClient orderExecutionClient,
        IClock clock,
        IOptions<QueuedOrderExecutionConfiguration> configuration,
        ILogger<QueuedOrderExecutionJob> logger)
    {
        _queuedOrderRepository = queuedOrderRepository;
        _marketOpenEvaluator = marketOpenEvaluator;
        _orderExecutionClient = orderExecutionClient;
        _clock = clock;
        _configuration = configuration.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!await _marketOpenEvaluator.IsMarketOpenAsync(cancellationToken))
        {
            _logger.LogInformation(
                "Skipping queued order execution because the market is not currently open.");
            return;
        }

        var queuedOrders = await _queuedOrderRepository.GetPendingQueuedOrdersAsync(
            _configuration.MaxBatchSize,
            cancellationToken);

        if (queuedOrders.Count == 0)
        {
            _logger.LogInformation("No pending queued orders were found for execution.");
            return;
        }

        _logger.LogInformation("Processing {Count} queued orders.", queuedOrders.Count);

        foreach (var queuedOrder in queuedOrders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessQueuedOrderAsync(queuedOrder, cancellationToken);
        }
    }

    private async Task ProcessQueuedOrderAsync(QueuedOrderEntity queuedOrder, CancellationToken cancellationToken)
    {
        var attemptStartedAtUtc = _clock.UtcNow;
        queuedOrder.Status = QueuedOrderExecutionConstants.InProgressStatus;
        queuedOrder.LastAttemptedAtUtc = attemptStartedAtUtc;
        queuedOrder.ProcessedAtUtc = null;
        queuedOrder.FailureMessage = null;
        queuedOrder.UpdatedAtUtc = attemptStartedAtUtc;
        await _queuedOrderRepository.SaveChangesAsync(cancellationToken);

        try
        {
            var response = await _orderExecutionClient.SubmitQueuedOrderAsync(queuedOrder, cancellationToken);
            if (response.Disposition != OrderDisposition.Executed)
            {
                throw new InvalidOperationException(
                    $"Order Execution returned disposition {response.Disposition} for queued order {queuedOrder.OrderId}.");
            }

            var completedAtUtc = _clock.UtcNow;
            queuedOrder.Status = QueuedOrderExecutionConstants.SucceededStatus;
            queuedOrder.ProcessedAtUtc = completedAtUtc;
            queuedOrder.FailureMessage = null;
            queuedOrder.UpdatedAtUtc = completedAtUtc;
            await _queuedOrderRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var failedAtUtc = _clock.UtcNow;
            queuedOrder.Status = QueuedOrderExecutionConstants.FailedStatus;
            queuedOrder.ProcessedAtUtc = failedAtUtc;
            queuedOrder.FailureMessage = Truncate(ex.Message);
            queuedOrder.UpdatedAtUtc = failedAtUtc;
            await _queuedOrderRepository.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex, "Failed to process queued order {QueuedOrderId}.", queuedOrder.OrderId);
        }
    }

    private static string Truncate(string message)
    {
        if (message.Length <= QueuedOrderExecutionConstants.FailureMessageMaxLength)
        {
            return message;
        }

        return message[..QueuedOrderExecutionConstants.FailureMessageMaxLength];
    }
}
