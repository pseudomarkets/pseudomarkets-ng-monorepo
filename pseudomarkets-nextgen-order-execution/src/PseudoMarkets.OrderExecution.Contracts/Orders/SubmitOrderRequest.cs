using System.ComponentModel.DataAnnotations;
using PseudoMarkets.OrderExecution.Contracts.Enums;

namespace PseudoMarkets.OrderExecution.Contracts.Orders;

/// <summary>
/// Request used to submit an order for execution or queueing.
/// </summary>
public sealed class SubmitOrderRequest
{
    /// <summary>
    /// Ten-digit Pseudo Markets user ID that owns the order.
    /// </summary>
    [Range(1000000000, 9999999999)]
    public long UserId { get; init; }

    /// <summary>
    /// Trading symbol to buy or sell.
    /// </summary>
    /// <example>AAPL</example>
    [Required]
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Order side, either Buy or Sell.
    /// </summary>
    [Required]
    public OrderSide Side { get; init; }

    /// <summary>
    /// Order quantity.
    /// </summary>
    [Range(typeof(decimal), "0.000001", "999999999999999.999999")]
    public decimal Quantity { get; init; }

    /// <summary>
    /// Order type. The current implementation supports Market orders.
    /// </summary>
    [Required]
    public OrderType OrderType { get; init; }
}
