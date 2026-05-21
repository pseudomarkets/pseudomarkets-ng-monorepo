using System.ComponentModel.DataAnnotations;

namespace PseudoMarkets.TransactionProcessing.Contracts.Transactions;

/// <summary>
/// Request used to post a cash deposit.
/// </summary>
public sealed class PostCashDepositRequest
{
    /// <summary>
    /// Caller-provided unique key used to make the request idempotent.
    /// </summary>
    [Required]
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>
    /// Ten-digit Pseudo Markets user ID that owns the cash balance.
    /// </summary>
    [Range(1000000000, 9999999999)]
    public long UserId { get; init; }

    /// <summary>
    /// Deposit amount to credit.
    /// </summary>
    [Range(typeof(decimal), "0.0001", "999999999999999.9999")]
    public decimal Amount { get; init; }

    /// <summary>
    /// UTC timestamp when the deposit occurred.
    /// </summary>
    public DateTime OccurredAtUtc { get; init; }

    /// <summary>
    /// External reference ID for the money movement.
    /// </summary>
    [Required]
    public string ExternalReferenceId { get; init; } = string.Empty;
}
