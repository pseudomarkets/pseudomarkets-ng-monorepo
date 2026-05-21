using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;
using PseudoMarkets.TransactionProcessing.Core.Interfaces;

namespace PseudoMarkets.TransactionProcessing.Service.Controllers;

[ApiController]
[Route("api/transactions")]
[AuthorizeWithIdentityServer(PlatformAuthorizationActions.UpdateTransactions)]
[Produces("application/json")]
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

    /// <summary>
    /// Posts an executed trade transaction.
    /// </summary>
    /// <remarks>
    /// Records a buy or sell trade, updates balances and positions, and calculates trade and settlement dates.
    /// Requires the UPDATE_TRANSACTIONS action.
    /// </remarks>
    [HttpPost("trades")]
    [EndpointSummary("Post trade transaction")]
    [EndpointDescription("Posts an executed trade transaction and updates the user's balance and position records.")]
    [ProducesResponseType<TransactionCommandResponse>(StatusCodes.Status200OK, Description = "The trade transaction was posted or an idempotent response was returned.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, Description = "The trade request failed validation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, Description = "The token is not authorized to update transactions.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, Description = "The transaction processor encountered an unexpected error.")]
    public async Task<ActionResult<TransactionCommandResponse>> PostTrade(
        [FromBody] PostTradeTransactionRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _tradeTransactionPostingService.PostTradeAsync(request, cancellationToken));
    }

    /// <summary>
    /// Voids a transaction.
    /// </summary>
    /// <remarks>
    /// Creates an offsetting transaction that reverses the balance and position impact of the original transaction.
    /// Requires the UPDATE_TRANSACTIONS action.
    /// </remarks>
    [HttpPost("{transactionId:guid}/void")]
    [EndpointSummary("Void transaction")]
    [EndpointDescription("Voids a posted transaction by creating an offsetting transaction and reversing its balance or position effects.")]
    [ProducesResponseType<TransactionCommandResponse>(StatusCodes.Status200OK, Description = "The transaction was voided or an idempotent response was returned.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, Description = "The void request failed validation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, Description = "The token is not authorized to update transactions.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Description = "The transaction to void was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, Description = "The transaction processor encountered an unexpected error.")]
    public async Task<ActionResult<TransactionCommandResponse>> VoidTransaction(
        Guid transactionId,
        [FromBody] VoidTransactionRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _voidTransactionService.VoidAsync(transactionId, request, cancellationToken));
    }
}
