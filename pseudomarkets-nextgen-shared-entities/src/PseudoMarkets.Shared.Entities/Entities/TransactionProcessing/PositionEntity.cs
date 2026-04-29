namespace PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;

public class PositionEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string PositionSide { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal SettledQuantity { get; set; }
    public decimal UnsettledQuantity { get; set; }
    public decimal CostBasisTotal { get; set; }
    public decimal SettledCostBasisTotal { get; set; }
    public decimal UnsettledCostBasisTotal { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
