using PseudoMarkets.OrderExecution.Contracts.Enums;

namespace PseudoMarkets.OrderExecution.Contracts.Orders;

public sealed class SubmitOrderResponse
{
    public required OrderDisposition Disposition { get; init; }
    public required Guid OrderId { get; init; }
    public Guid? ExecutionId { get; init; }
    public Guid? TransactionId { get; init; }
    public Guid? PostingBatchId { get; init; }
    public required long UserId { get; init; }
    public required string Symbol { get; init; }
    public required OrderSide Side { get; init; }
    public required OrderType OrderType { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? FillPrice { get; init; }
    public decimal? GrossAmount { get; init; }
    public decimal? Fees { get; init; }
    public decimal? NetAmount { get; init; }
    public required OrderStatus Status { get; init; }
    public required DateTime SubmittedAtUtc { get; init; }
    public DateTime? ExecutedAtUtc { get; init; }
}
