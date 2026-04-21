namespace PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;

public class CashMovementEntity
{
    public long Id { get; set; }
    public Guid TransactionId { get; set; }
    public long UserId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string? ExternalReferenceId { get; set; }
    public string? ReasonCode { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
