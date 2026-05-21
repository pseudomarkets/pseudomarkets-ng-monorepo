using System.ComponentModel.DataAnnotations;
using PseudoMarkets.TransactionProcessing.Contracts.Enums;

namespace PseudoMarkets.TransactionProcessing.Contracts.Transactions;

/// <summary>
/// Request used to post an operational cash adjustment.
/// </summary>
public sealed class PostCashAdjustmentRequest
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
    /// Adjustment amount to credit or debit.
    /// </summary>
    [Range(typeof(decimal), "0.0001", "999999999999999.9999")]
    public decimal Amount { get; init; }

    /// <summary>
    /// Direction of the adjustment.
    /// </summary>
    [Required]
    public CashAdjustmentDirection Direction { get; init; }

    /// <summary>
    /// UTC timestamp when the adjustment occurred.
    /// </summary>
    public DateTime OccurredAtUtc { get; init; }

    /// <summary>
    /// Operational reason code for the adjustment.
    /// </summary>
    [Required]
    public string ReasonCode { get; init; } = string.Empty;
}
