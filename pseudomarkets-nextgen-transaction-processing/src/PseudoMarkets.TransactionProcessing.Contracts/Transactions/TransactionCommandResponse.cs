namespace PseudoMarkets.TransactionProcessing.Contracts.Transactions;

public sealed class TransactionCommandResponse
{
    public required Guid PostingBatchId { get; init; }
    public required Guid TransactionId { get; init; }
    public required string Status { get; init; }
    public required string TransactionDescription { get; init; }
    public string? Message { get; init; }
}
