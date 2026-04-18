using System.ComponentModel.DataAnnotations;
using PseudoMarkets.TransactionProcessing.Contracts.Enums;

namespace PseudoMarkets.TransactionProcessing.Contracts.Transactions;

public sealed class PostTradeTransactionRequest
{
    [Required]
    public string IdempotencyKey { get; init; } = string.Empty;

    [Range(1000000000, 9999999999)]
    public long UserId { get; init; }

    [Required]
    public string Symbol { get; init; } = string.Empty;

    [Required]
    public TradeSide TradeSide { get; init; }

    [Range(typeof(decimal), "0.000001", "999999999999999.999999")]
    public decimal Quantity { get; init; }

    [Range(typeof(decimal), "0.000001", "999999999999999.999999")]
    public decimal ExecutionPrice { get; init; }

    [Range(typeof(decimal), "0.0001", "999999999999999.9999")]
    public decimal GrossAmount { get; init; }

    [Range(typeof(decimal), "0.0000", "999999999999999.9999")]
    public decimal Fees { get; init; }

    [Range(typeof(decimal), "0.0001", "999999999999999.9999")]
    public decimal NetAmount { get; init; }

    public DateTime ExecutedAtUtc { get; init; }

    [Required]
    public string ExternalExecutionId { get; init; } = string.Empty;
}
