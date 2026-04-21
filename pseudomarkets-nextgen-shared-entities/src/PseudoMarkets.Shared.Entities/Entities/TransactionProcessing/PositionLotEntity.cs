namespace PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;

public class PositionLotEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public Guid OpeningTransactionId { get; set; }
    public Guid? ClosingTransactionId { get; set; }
    public string LotEntryType { get; set; } = string.Empty;
    public decimal QuantityOpened { get; set; }
    public decimal QuantityRemaining { get; set; }
    public decimal Price { get; set; }
    public DateTime OpenedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<PositionLotClosureEntity> Closures { get; set; } = [];
}
