namespace PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;

public class PositionEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string PositionSide { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal CostBasisTotal { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
