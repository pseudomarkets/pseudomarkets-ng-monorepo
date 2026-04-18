namespace PseudoMarkets.TransactionProcessing.Persistence.Entities;

public class LedgerTransactionEntity
{
    public long Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid PostingBatchId { get; set; }
    public long UserId { get; set; }
    public string TransactionKind { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string TransactionDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public Guid? VoidsTransactionId { get; set; }
    public string? ExternalReferenceId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public PostingBatchEntity PostingBatch { get; set; } = null!;
}
