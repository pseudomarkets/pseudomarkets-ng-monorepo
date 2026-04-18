using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;
using PseudoMarkets.TransactionProcessing.Core.Interfaces;

namespace PseudoMarkets.TransactionProcessing.Service.Controllers;

[ApiController]
[Route("api/transactions")]
[AuthorizeWithIdentityServer(PlatformAuthorizationActions.UpdateTransactions)]
public class TransactionsController : ControllerBase
{
    private readonly ITradeTransactionPostingService _tradeTransactionPostingService;
    private readonly IVoidTransactionService _voidTransactionService;

    public TransactionsController(
        ITradeTransactionPostingService tradeTransactionPostingService,
        IVoidTransactionService voidTransactionService)
    {
        _tradeTransactionPostingService = tradeTransactionPostingService;
        _voidTransactionService = voidTransactionService;
    }

    [HttpPost("trades")]
    public async Task<ActionResult<TransactionCommandResponse>> PostTrade(
        [FromBody] PostTradeTransactionRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _tradeTransactionPostingService.PostTradeAsync(request, cancellationToken));
    }

    [HttpPost("{transactionId:guid}/void")]
    public async Task<ActionResult<TransactionCommandResponse>> VoidTransaction(
        Guid transactionId,
        [FromBody] VoidTransactionRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _voidTransactionService.VoidAsync(transactionId, request, cancellationToken));
    }
}
