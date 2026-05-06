using System.ComponentModel.DataAnnotations;
using PseudoMarkets.OrderExecution.Contracts.Enums;

namespace PseudoMarkets.OrderExecution.Contracts.Orders;

public sealed class SubmitOrderRequest
{
    [Range(1000000000, 9999999999)]
    public long UserId { get; init; }

    [Required]
    public string Symbol { get; init; } = string.Empty;

    [Required]
    public OrderSide Side { get; init; }

    [Range(typeof(decimal), "0.000001", "999999999999999.999999")]
    public decimal Quantity { get; init; }

    [Required]
    public OrderType OrderType { get; init; }
}
