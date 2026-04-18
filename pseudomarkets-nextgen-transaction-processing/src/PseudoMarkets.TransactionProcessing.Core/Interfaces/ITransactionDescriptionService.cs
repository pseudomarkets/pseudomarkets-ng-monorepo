using PseudoMarkets.TransactionProcessing.Contracts.Enums;

namespace PseudoMarkets.TransactionProcessing.Core.Interfaces;

public interface ITransactionDescriptionService
{
    string DescribeTrade(TransactionKind transactionKind, string symbol, decimal amount);
    string DescribeCashMovement(TransactionKind transactionKind, decimal amount, CashAdjustmentDirection? adjustmentDirection = null);
    string DescribeVoid(string originalDescription);
}
