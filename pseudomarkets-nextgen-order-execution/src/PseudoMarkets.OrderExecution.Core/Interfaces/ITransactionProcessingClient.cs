using PseudoMarkets.TransactionProcessing.Contracts.Transactions;

namespace PseudoMarkets.OrderExecution.Core.Interfaces;

public interface ITransactionProcessingClient
{
    Task<TransactionCommandResponse> PostTradeAsync(
        PostTradeTransactionRequest request,
        CancellationToken cancellationToken);
}
