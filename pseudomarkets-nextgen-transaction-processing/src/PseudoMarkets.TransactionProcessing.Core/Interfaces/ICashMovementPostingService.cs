using PseudoMarkets.TransactionProcessing.Contracts.Transactions;

namespace PseudoMarkets.TransactionProcessing.Core.Interfaces;

public interface ICashMovementPostingService
{
    Task<TransactionCommandResponse> PostDepositAsync(PostCashDepositRequest request, CancellationToken cancellationToken = default);
    Task<TransactionCommandResponse> PostWithdrawalAsync(PostCashWithdrawalRequest request, CancellationToken cancellationToken = default);
    Task<TransactionCommandResponse> PostAdjustmentAsync(PostCashAdjustmentRequest request, CancellationToken cancellationToken = default);
}
