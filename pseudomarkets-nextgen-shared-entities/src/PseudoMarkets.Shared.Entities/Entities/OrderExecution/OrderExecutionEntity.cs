namespace PseudoMarkets.Shared.Entities.Entities.OrderExecution;

public class OrderExecutionEntity
{
    public Guid OrderId { get; set; }
    public Guid ExecutionId { get; set; }
    public long UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string OrderSide { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal FillPrice { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal Fees { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? TransactionId { get; set; }
    public Guid? PostingBatchId { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? ExecutedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
