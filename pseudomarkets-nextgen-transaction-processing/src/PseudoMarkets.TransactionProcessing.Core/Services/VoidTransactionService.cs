using PseudoMarkets.TransactionProcessing.Contracts.Transactions;
using PseudoMarkets.TransactionProcessing.Core.Interfaces;

namespace PseudoMarkets.TransactionProcessing.Core.Services;

public class VoidTransactionService : IVoidTransactionService
{
    public Task<TransactionCommandResponse> VoidAsync(Guid transactionId, VoidTransactionRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TransactionCommandResponse
        {
            PostingBatchId = Guid.NewGuid(),
            TransactionId = Guid.NewGuid(),
            Status = "Scaffolded",
            TransactionDescription = $"VOID TRANSACTION {transactionId:D}",
            Message = "Scaffold placeholder: transaction lookup and compensating entries will be implemented next."
        });
    }
}
