namespace PseudoMarkets.TransactionProcessing.Persistence.Entities;

public class AccountBalanceEntity
{
    public long UserId { get; set; }
    public decimal CashBalance { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
