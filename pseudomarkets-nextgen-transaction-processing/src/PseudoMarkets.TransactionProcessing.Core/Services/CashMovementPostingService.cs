using PseudoMarkets.TransactionProcessing.Contracts.Enums;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;
using PseudoMarkets.TransactionProcessing.Core.Interfaces;

namespace PseudoMarkets.TransactionProcessing.Core.Services;

public class CashMovementPostingService : ICashMovementPostingService
{
    private readonly ITransactionDescriptionService _transactionDescriptionService;

    public CashMovementPostingService(ITransactionDescriptionService transactionDescriptionService)
    {
        _transactionDescriptionService = transactionDescriptionService;
    }

    public Task<TransactionCommandResponse> PostDepositAsync(PostCashDepositRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreatePlaceholderResponse(
            _transactionDescriptionService.DescribeCashMovement(TransactionKind.CashDeposit, request.Amount)));
    }

    public Task<TransactionCommandResponse> PostWithdrawalAsync(PostCashWithdrawalRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreatePlaceholderResponse(
            _transactionDescriptionService.DescribeCashMovement(TransactionKind.CashWithdrawal, request.Amount)));
    }

    public Task<TransactionCommandResponse> PostAdjustmentAsync(PostCashAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreatePlaceholderResponse(
            _transactionDescriptionService.DescribeCashMovement(TransactionKind.CashAdjustment, request.Amount, request.Direction)));
    }

    private static TransactionCommandResponse CreatePlaceholderResponse(string description)
    {
        return new TransactionCommandResponse
        {
            PostingBatchId = Guid.NewGuid(),
            TransactionId = Guid.NewGuid(),
            Status = "Scaffolded",
            TransactionDescription = description,
            Message = "Scaffold placeholder: posting persistence will be implemented next."
        };
    }
}
