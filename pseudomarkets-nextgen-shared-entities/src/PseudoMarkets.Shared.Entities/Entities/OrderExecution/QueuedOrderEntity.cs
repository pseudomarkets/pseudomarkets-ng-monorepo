namespace PseudoMarkets.Shared.Entities.Entities.OrderExecution;

public class QueuedOrderEntity
{
    public Guid OrderId { get; set; }
    public long UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string OrderSide { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string QueueReason { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? LastAttemptedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
