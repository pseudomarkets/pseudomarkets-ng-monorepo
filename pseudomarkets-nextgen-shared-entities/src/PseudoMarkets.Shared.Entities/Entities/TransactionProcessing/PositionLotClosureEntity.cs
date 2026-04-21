namespace PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;

public class PositionLotClosureEntity
{
    public long Id { get; set; }
    public long PositionLotId { get; set; }
    public Guid OpeningTransactionId { get; set; }
    public Guid ClosingTransactionId { get; set; }
    public long UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal QuantityClosed { get; set; }
    public decimal CostBasisAmount { get; set; }
    public DateTime ClosedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public PositionLotEntity PositionLot { get; set; } = null!;
}
