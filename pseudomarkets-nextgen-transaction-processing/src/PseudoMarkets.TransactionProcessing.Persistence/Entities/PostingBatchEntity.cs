namespace PseudoMarkets.TransactionProcessing.Persistence.Entities;

public class PostingBatchEntity
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }

    public ICollection<LedgerTransactionEntity> LedgerTransactions { get; set; } = [];
}
