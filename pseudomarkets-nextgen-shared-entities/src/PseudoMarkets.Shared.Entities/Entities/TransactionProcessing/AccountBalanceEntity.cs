namespace PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;

public class AccountBalanceEntity
{
    public long UserId { get; set; }
    public decimal CashBalance { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
