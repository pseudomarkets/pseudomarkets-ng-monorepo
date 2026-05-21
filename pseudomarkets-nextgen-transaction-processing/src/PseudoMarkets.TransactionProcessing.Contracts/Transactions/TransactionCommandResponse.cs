namespace PseudoMarkets.TransactionProcessing.Contracts.Transactions;

/// <summary>
/// Response returned after posting or voiding a transaction.
/// </summary>
public sealed class TransactionCommandResponse
{
    /// <summary>
    /// Posting batch identifier for the command.
    /// </summary>
    public required Guid PostingBatchId { get; init; }

    /// <summary>
    /// Transaction identifier created or affected by the command.
    /// </summary>
    public required Guid TransactionId { get; init; }

    /// <summary>
    /// Final command status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Auto-generated transaction description.
    /// </summary>
    public required string TransactionDescription { get; init; }

    /// <summary>
    /// Optional informational message.
    /// </summary>
    public string? Message { get; init; }
}
