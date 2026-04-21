namespace PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;

public class TradeExecutionEntity
{
    public long Id { get; set; }
    public Guid TransactionId { get; set; }
    public long UserId { get; set; }
    public string ExternalExecutionId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string TradeSide { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ExecutionPrice { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal Fees { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime ExecutedAtUtc { get; set; }
    public DateOnly TradeDate { get; set; }
    public DateOnly SettlementDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
