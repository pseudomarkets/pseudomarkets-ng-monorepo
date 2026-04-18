using System.ComponentModel.DataAnnotations;

namespace PseudoMarkets.TransactionProcessing.Contracts.Transactions;

public sealed class VoidTransactionRequest
{
    [Required]
    public string IdempotencyKey { get; init; } = string.Empty;

    public DateTime VoidedAtUtc { get; init; }

    [Required]
    public string ReasonCode { get; init; } = string.Empty;
}
