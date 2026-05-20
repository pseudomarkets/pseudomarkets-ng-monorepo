using PseudoMarkets.OrderExecution.Contracts.Orders;
using PseudoMarkets.Shared.Entities.Entities.OrderExecution;

namespace PseudoMarkets.Platform.Batch.Host.Interfaces;

internal interface IOrderExecutionClient
{
    Task<SubmitOrderResponse> SubmitQueuedOrderAsync(QueuedOrderEntity queuedOrder, CancellationToken cancellationToken);
}
