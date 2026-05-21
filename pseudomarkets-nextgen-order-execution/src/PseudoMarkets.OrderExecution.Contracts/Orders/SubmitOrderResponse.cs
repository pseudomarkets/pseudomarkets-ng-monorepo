using PseudoMarkets.OrderExecution.Contracts.Enums;

namespace PseudoMarkets.OrderExecution.Contracts.Orders;

/// <summary>
/// Response returned after submitting an order.
/// </summary>
public sealed class SubmitOrderResponse
{
    /// <summary>
    /// Indicates whether the order was executed immediately or queued for later execution.
    /// </summary>
    public required OrderDisposition Disposition { get; init; }

    /// <summary>
    /// Platform order identifier.
    /// </summary>
    public required Guid OrderId { get; init; }

    /// <summary>
    /// Execution identifier when the order was filled immediately.
    /// </summary>
    public Guid? ExecutionId { get; init; }

    /// <summary>
    /// Transaction identifier returned by Transaction Processing when a fill was posted.
    /// </summary>
    public Guid? TransactionId { get; init; }

    /// <summary>
    /// Posting batch identifier returned by Transaction Processing when a fill was posted.
    /// </summary>
    public Guid? PostingBatchId { get; init; }

    /// <summary>
    /// Ten-digit Pseudo Markets user ID that owns the order.
    /// </summary>
    public required long UserId { get; init; }

    /// <summary>
    /// Trading symbol submitted on the order.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Submitted order side.
    /// </summary>
    public required OrderSide Side { get; init; }

    /// <summary>
    /// Submitted order type.
    /// </summary>
    public required OrderType OrderType { get; init; }

    /// <summary>
    /// Submitted order quantity.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Fill price when the order was executed.
    /// </summary>
    public decimal? FillPrice { get; init; }

    /// <summary>
    /// Gross execution amount when the order was executed.
    /// </summary>
    public decimal? GrossAmount { get; init; }

    /// <summary>
    /// Execution fees when the order was executed.
    /// </summary>
    public decimal? Fees { get; init; }

    /// <summary>
    /// Net cash impact when the order was executed.
    /// </summary>
    public decimal? NetAmount { get; init; }

    /// <summary>
    /// Final order status after submission.
    /// </summary>
    public required OrderStatus Status { get; init; }

    /// <summary>
    /// UTC timestamp when the order was submitted.
    /// </summary>
    public required DateTime SubmittedAtUtc { get; init; }

    /// <summary>
    /// UTC timestamp when the order was executed, if applicable.
    /// </summary>
    public DateTime? ExecutedAtUtc { get; init; }
}
