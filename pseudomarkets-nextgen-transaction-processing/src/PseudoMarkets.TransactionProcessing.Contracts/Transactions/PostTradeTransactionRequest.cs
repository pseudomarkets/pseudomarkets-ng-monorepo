using System.ComponentModel.DataAnnotations;
using PseudoMarkets.TransactionProcessing.Contracts.Enums;

namespace PseudoMarkets.TransactionProcessing.Contracts.Transactions;

/// <summary>
/// Request used to post an executed trade transaction.
/// </summary>
public sealed class PostTradeTransactionRequest
{
    /// <summary>
    /// Caller-provided unique key used to make the request idempotent.
    /// </summary>
    [Required]
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>
    /// Ten-digit Pseudo Markets user ID that owns the trade.
    /// </summary>
    [Range(1000000000, 9999999999)]
    public long UserId { get; init; }

    /// <summary>
    /// Executed trading symbol.
    /// </summary>
    /// <example>AAPL</example>
    [Required]
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Trade side, either Buy or Sell.
    /// </summary>
    [Required]
    public TradeSide TradeSide { get; init; }

    /// <summary>
    /// Executed share or unit quantity.
    /// </summary>
    [Range(typeof(decimal), "0.000001", "999999999999999.999999")]
    public decimal Quantity { get; init; }

    /// <summary>
    /// Execution price per share or unit.
    /// </summary>
    [Range(typeof(decimal), "0.000001", "999999999999999.999999")]
    public decimal ExecutionPrice { get; init; }

    /// <summary>
    /// Gross transaction amount before fees.
    /// </summary>
    [Range(typeof(decimal), "0.0001", "999999999999999.9999")]
    public decimal GrossAmount { get; init; }

    /// <summary>
    /// Fees applied to the execution.
    /// </summary>
    [Range(typeof(decimal), "0.0000", "999999999999999.9999")]
    public decimal Fees { get; init; }

    /// <summary>
    /// Net cash impact of the trade after fees.
    /// </summary>
    [Range(typeof(decimal), "0.0001", "999999999999999.9999")]
    public decimal NetAmount { get; init; }

    /// <summary>
    /// UTC timestamp when the trade was executed.
    /// </summary>
    public DateTime ExecutedAtUtc { get; init; }

    /// <summary>
    /// External execution ID supplied by the order execution system.
    /// </summary>
    [Required]
    public string ExternalExecutionId { get; init; } = string.Empty;
}
