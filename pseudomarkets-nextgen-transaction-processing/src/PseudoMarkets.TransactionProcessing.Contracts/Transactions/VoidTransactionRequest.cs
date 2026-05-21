using System.ComponentModel.DataAnnotations;

namespace PseudoMarkets.TransactionProcessing.Contracts.Transactions;

/// <summary>
/// Request used to void a posted transaction.
/// </summary>
public sealed class VoidTransactionRequest
{
    /// <summary>
    /// Caller-provided unique key used to make the void request idempotent.
    /// </summary>
    [Required]
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the transaction was voided.
    /// </summary>
    public DateTime VoidedAtUtc { get; init; }

    /// <summary>
    /// Reason code explaining why the transaction was voided.
    /// </summary>
    [Required]
    public string ReasonCode { get; init; } = string.Empty;
}
