using System.Globalization;
using PseudoMarkets.TransactionProcessing.Contracts.Enums;
using PseudoMarkets.TransactionProcessing.Core.Interfaces;

namespace PseudoMarkets.TransactionProcessing.Core.Services;

public class TransactionDescriptionService : ITransactionDescriptionService
{
    public string DescribeTrade(TransactionKind transactionKind, string symbol, decimal amount)
    {
        var action = transactionKind switch
        {
            TransactionKind.TradeBuy => "TRADE BUY",
            TransactionKind.TradeSell => "TRADE SELL",
            _ => throw new ArgumentOutOfRangeException(nameof(transactionKind), transactionKind, null)
        };

        return $"{action} {symbol.ToUpperInvariant()} {FormatAmount(amount)}";
    }

    public string DescribeCashMovement(TransactionKind transactionKind, decimal amount, CashAdjustmentDirection? adjustmentDirection = null)
    {
        return transactionKind switch
        {
            TransactionKind.CashDeposit => $"CASH DEPOSIT {FormatAmount(amount)}",
            TransactionKind.CashWithdrawal => $"CASH WITHDRAWAL {FormatAmount(amount)}",
            TransactionKind.CashAdjustment => $"CASH ADJUSTMENT {adjustmentDirection?.ToString().ToUpperInvariant()} {FormatAmount(amount)}",
            _ => throw new ArgumentOutOfRangeException(nameof(transactionKind), transactionKind, null)
        };
    }

    public string DescribeVoid(string originalDescription)
    {
        return $"VOID {originalDescription}";
    }

    private static string FormatAmount(decimal amount)
    {
        return amount.ToString("$0.00", CultureInfo.InvariantCulture);
    }
}
