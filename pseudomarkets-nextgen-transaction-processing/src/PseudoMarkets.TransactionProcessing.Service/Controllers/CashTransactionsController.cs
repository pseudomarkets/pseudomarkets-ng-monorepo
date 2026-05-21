using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;
using PseudoMarkets.TransactionProcessing.Core.Interfaces;

namespace PseudoMarkets.TransactionProcessing.Service.Controllers;

[ApiController]
[Route("api/transactions/cash")]
[AuthorizeWithIdentityServer(PlatformAuthorizationActions.UpdateTransactions)]
[Produces("application/json")]
public class CashTransactionsController : ControllerBase
{
    private readonly ICashMovementPostingService _cashMovementPostingService;

    public CashTransactionsController(ICashMovementPostingService cashMovementPostingService)
    {
        _cashMovementPostingService = cashMovementPostingService;
    }

    /// <summary>
    /// Posts a cash deposit.
    /// </summary>
    /// <remarks>
    /// Credits a user's cash balance for an external cash deposit. Requires the UPDATE_TRANSACTIONS action.
    /// </remarks>
    [HttpPost("deposit")]
    [EndpointSummary("Post cash deposit")]
    [EndpointDescription("Posts a cash deposit and credits the user's cash balance.")]
    [ProducesResponseType<TransactionCommandResponse>(StatusCodes.Status200OK, Description = "The cash deposit was posted or an idempotent response was returned.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, Description = "The cash deposit request failed validation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, Description = "The token is not authorized to update transactions.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, Description = "The transaction processor encountered an unexpected error.")]
    public async Task<ActionResult<TransactionCommandResponse>> Deposit(
        [FromBody] PostCashDepositRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _cashMovementPostingService.PostDepositAsync(request, cancellationToken));
    }

    /// <summary>
    /// Posts a cash withdrawal.
    /// </summary>
    /// <remarks>
    /// Debits a user's cash balance for an external cash withdrawal. Requires the UPDATE_TRANSACTIONS action.
    /// </remarks>
    [HttpPost("withdrawal")]
    [EndpointSummary("Post cash withdrawal")]
    [EndpointDescription("Posts a cash withdrawal and debits the user's cash balance.")]
    [ProducesResponseType<TransactionCommandResponse>(StatusCodes.Status200OK, Description = "The cash withdrawal was posted or an idempotent response was returned.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, Description = "The cash withdrawal request failed validation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, Description = "The token is not authorized to update transactions.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, Description = "The transaction processor encountered an unexpected error.")]
    public async Task<ActionResult<TransactionCommandResponse>> Withdrawal(
        [FromBody] PostCashWithdrawalRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _cashMovementPostingService.PostWithdrawalAsync(request, cancellationToken));
    }

    /// <summary>
    /// Posts a cash adjustment.
    /// </summary>
    /// <remarks>
    /// Credits or debits a user's cash balance for an operational adjustment. Requires the UPDATE_TRANSACTIONS action.
    /// </remarks>
    [HttpPost("adjustment")]
    [EndpointSummary("Post cash adjustment")]
    [EndpointDescription("Posts a cash adjustment and credits or debits the user's cash balance.")]
    [ProducesResponseType<TransactionCommandResponse>(StatusCodes.Status200OK, Description = "The cash adjustment was posted or an idempotent response was returned.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, Description = "The cash adjustment request failed validation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, Description = "The token is not authorized to update transactions.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, Description = "The transaction processor encountered an unexpected error.")]
    public async Task<ActionResult<TransactionCommandResponse>> Adjustment(
        [FromBody] PostCashAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _cashMovementPostingService.PostAdjustmentAsync(request, cancellationToken));
    }
}
