using PseudoMarkets.TransactionProcessing.Contracts.Transactions;

namespace PseudoMarkets.TransactionProcessing.Core.Interfaces;

public interface ITradeTransactionPostingService
{
    Task<TransactionCommandResponse> PostTradeAsync(PostTradeTransactionRequest request, CancellationToken cancellationToken = default);
}
