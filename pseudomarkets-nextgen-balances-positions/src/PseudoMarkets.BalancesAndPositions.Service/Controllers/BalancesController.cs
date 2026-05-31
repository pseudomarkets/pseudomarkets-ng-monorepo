using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.BalancesAndPositions.Contracts.Requests;
using PseudoMarkets.BalancesAndPositions.Contracts.Responses;
using PseudoMarkets.BalancesAndPositions.Core.Exceptions;
using PseudoMarkets.BalancesAndPositions.Core.Interfaces;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;
using PseudoMarkets.Shared.Authorization.Models;

namespace PseudoMarkets.BalancesAndPositions.Service.Controllers;

[ApiController]
[Route("balances")]
[AuthorizeWithIdentityServer(PlatformAuthorizationActions.ViewTransactions)]
[Produces("application/json")]
public sealed class BalancesController : ControllerBase
{
    private readonly IBalanceQueryService _balanceQueryService;

    public BalancesController(IBalanceQueryService balanceQueryService)
    {
        _balanceQueryService = balanceQueryService;
    }

    /// <summary>
    /// Gets a user's balance snapshot.
    /// </summary>
    /// <remarks>
    /// Returns aggregate, settled, and unsettled cash balances for the requested user. USER tokens may only request
    /// their own user ID. SYSTEM tokens may request any user ID. Requires the VIEW_TRANSACTIONS action.
    /// </remarks>
    [HttpPost]
    [EndpointSummary("Get balances")]
    [EndpointDescription("Returns the requested user's aggregate, settled, and unsettled cash balances.")]
    [ProducesResponseType<BalanceQueryResponse>(StatusCodes.Status200OK, Description = "The balance snapshot was returned.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, Description = "The balance request failed validation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, Description = "The caller is not authorized to request balances for the specified user.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Description = "No balance record exists for the requested user.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, Description = "The balances and positions service encountered an unexpected error.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, Description = "The balances and positions service could not reach a required dependency.")]
    public async Task<ActionResult<BalanceQueryResponse>> Post(
        [FromBody] BalanceQueryRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequestOwnership(request.UserId);
        ValidateView(request.View);

        return Ok(await _balanceQueryService.GetBalanceAsync(request, cancellationToken));
    }

    private void ValidateRequestOwnership(long requestedUserId)
    {
        var tokenType = GetAuthorizedTokenType();
        if (string.Equals(tokenType, PlatformTokenTypes.System, StringComparison.Ordinal))
        {
            return;
        }

        var authorizedUserId = GetAuthorizedUserId();
        if (authorizedUserId != requestedUserId)
        {
            throw new BalancesAndPositionsForbiddenException(
                "The authenticated token cannot request balances for a different user.");
        }
    }

    private void ValidateView(Contracts.Enums.PositionView? view)
    {
        if (view.HasValue && !Enum.IsDefined(view.Value))
        {
            throw new BalancesAndPositionsValidationException("The requested view is invalid.");
        }
    }

    private long GetAuthorizedUserId()
    {
        if (HttpContext.Items.TryGetValue(AuthorizedIdentityContext.UserIdItemKey, out var value) &&
            value is long userId)
        {
            return userId;
        }

        throw new BalancesAndPositionsForbiddenException("The authenticated token did not include a valid user context.");
    }

    private string GetAuthorizedTokenType()
    {
        if (HttpContext.Items.TryGetValue(AuthorizedIdentityContext.TokenTypeItemKey, out var value) &&
            value is string tokenType &&
            !string.IsNullOrWhiteSpace(tokenType))
        {
            return tokenType;
        }

        throw new BalancesAndPositionsForbiddenException("The authenticated token did not include a valid token type.");
    }
}
