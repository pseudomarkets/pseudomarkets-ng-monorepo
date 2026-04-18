using PseudoMarkets.TransactionProcessing.Contracts.Enums;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;
using PseudoMarkets.TransactionProcessing.Core.Interfaces;

namespace PseudoMarkets.TransactionProcessing.Core.Services;

public class TradeTransactionPostingService : ITradeTransactionPostingService
{
    private readonly ITransactionDescriptionService _transactionDescriptionService;

    public TradeTransactionPostingService(ITransactionDescriptionService transactionDescriptionService)
    {
        _transactionDescriptionService = transactionDescriptionService;
    }

    public Task<TransactionCommandResponse> PostTradeAsync(PostTradeTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var kind = request.TradeSide switch
        {
            TradeSide.Buy => TransactionKind.TradeBuy,
            TradeSide.Sell => TransactionKind.TradeSell,
            _ => throw new ArgumentOutOfRangeException(nameof(request.TradeSide), request.TradeSide, null)
        };

        return Task.FromResult(new TransactionCommandResponse
        {
            PostingBatchId = Guid.NewGuid(),
            TransactionId = Guid.NewGuid(),
            Status = "Scaffolded",
            TransactionDescription = _transactionDescriptionService.DescribeTrade(kind, request.Symbol, request.NetAmount),
            Message = "Scaffold placeholder: posting persistence will be implemented next."
        });
    }
}
