namespace PseudoMarkets.TransactionProcessing.Contracts.Enums;

public enum TransactionKind
{
    TradeBuy = 1,
    TradeSell = 2,
    CashDeposit = 3,
    CashWithdrawal = 4,
    CashAdjustment = 5,
    Void = 6
}
