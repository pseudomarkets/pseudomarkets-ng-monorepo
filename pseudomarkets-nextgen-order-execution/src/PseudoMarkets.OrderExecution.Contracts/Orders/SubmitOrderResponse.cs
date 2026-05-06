using PseudoMarkets.OrderExecution.Contracts.Enums;

namespace PseudoMarkets.OrderExecution.Contracts.Orders;

public sealed class SubmitOrderResponse
{
    public required Guid OrderId { get; init; }
    public required Guid ExecutionId { get; init; }
    public Guid? TransactionId { get; init; }
    public Guid? PostingBatchId { get; init; }
    public required long UserId { get; init; }
    public required string Symbol { get; init; }
    public required OrderSide Side { get; init; }
    public required OrderType OrderType { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal FillPrice { get; init; }
    public required decimal GrossAmount { get; init; }
    public required decimal Fees { get; init; }
    public required decimal NetAmount { get; init; }
    public required OrderStatus Status { get; init; }
    public required DateTime SubmittedAtUtc { get; init; }
    public DateTime? ExecutedAtUtc { get; init; }
}
