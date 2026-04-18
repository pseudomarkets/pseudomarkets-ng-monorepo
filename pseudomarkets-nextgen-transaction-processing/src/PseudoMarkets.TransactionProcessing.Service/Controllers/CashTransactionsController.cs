using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;
using PseudoMarkets.TransactionProcessing.Core.Interfaces;

namespace PseudoMarkets.TransactionProcessing.Service.Controllers;

[ApiController]
[Route("api/transactions/cash")]
[AuthorizeWithIdentityServer(PlatformAuthorizationActions.UpdateTransactions)]
public class CashTransactionsController : ControllerBase
{
    private readonly ICashMovementPostingService _cashMovementPostingService;

    public CashTransactionsController(ICashMovementPostingService cashMovementPostingService)
    {
        _cashMovementPostingService = cashMovementPostingService;
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<TransactionCommandResponse>> Deposit(
        [FromBody] PostCashDepositRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _cashMovementPostingService.PostDepositAsync(request, cancellationToken));
    }

    [HttpPost("withdrawal")]
    public async Task<ActionResult<TransactionCommandResponse>> Withdrawal(
        [FromBody] PostCashWithdrawalRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _cashMovementPostingService.PostWithdrawalAsync(request, cancellationToken));
    }

    [HttpPost("adjustment")]
    public async Task<ActionResult<TransactionCommandResponse>> Adjustment(
        [FromBody] PostCashAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _cashMovementPostingService.PostAdjustmentAsync(request, cancellationToken));
    }
}
