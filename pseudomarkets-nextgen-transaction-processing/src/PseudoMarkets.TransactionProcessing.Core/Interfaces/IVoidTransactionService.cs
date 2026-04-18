using PseudoMarkets.TransactionProcessing.Contracts.Transactions;

namespace PseudoMarkets.TransactionProcessing.Core.Interfaces;

public interface IVoidTransactionService
{
    Task<TransactionCommandResponse> VoidAsync(Guid transactionId, VoidTransactionRequest request, CancellationToken cancellationToken = default);
}
