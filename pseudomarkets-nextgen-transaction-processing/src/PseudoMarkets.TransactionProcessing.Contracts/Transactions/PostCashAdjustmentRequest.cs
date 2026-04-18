using System.ComponentModel.DataAnnotations;
using PseudoMarkets.TransactionProcessing.Contracts.Enums;

namespace PseudoMarkets.TransactionProcessing.Contracts.Transactions;

public sealed class PostCashAdjustmentRequest
{
    [Required]
    public string IdempotencyKey { get; init; } = string.Empty;

    [Range(1000000000, 9999999999)]
    public long UserId { get; init; }

    [Range(typeof(decimal), "0.0001", "999999999999999.9999")]
    public decimal Amount { get; init; }

    [Required]
    public CashAdjustmentDirection Direction { get; init; }

    public DateTime OccurredAtUtc { get; init; }

    [Required]
    public string ReasonCode { get; init; } = string.Empty;
}
