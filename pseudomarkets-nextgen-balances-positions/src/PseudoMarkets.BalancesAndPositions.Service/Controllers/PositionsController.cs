using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.BalancesAndPositions.Contracts.Enums;
using PseudoMarkets.BalancesAndPositions.Contracts.Requests;
using PseudoMarkets.BalancesAndPositions.Contracts.Responses;
using PseudoMarkets.BalancesAndPositions.Core.Exceptions;
using PseudoMarkets.BalancesAndPositions.Core.Interfaces;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;
using PseudoMarkets.Shared.Authorization.Models;

namespace PseudoMarkets.BalancesAndPositions.Service.Controllers;

[ApiController]
[Route("positions")]
[AuthorizeWithIdentityServer(PlatformAuthorizationActions.ViewTransactions)]
[Produces("application/json")]
public sealed class PositionsController : ControllerBase
{
    private readonly IPositionQueryService _positionQueryService;

    public PositionsController(IPositionQueryService positionQueryService)
    {
        _positionQueryService = positionQueryService;
    }

    /// <summary>
    /// Gets a user's position snapshot.
    /// </summary>
    /// <remarks>
    /// Returns aggregate, settled, and unsettled position quantities and cost basis for the requested user, enriched
    /// with current market value and unrealized gain/loss where quote data is available. USER tokens may only request
    /// their own user ID. SYSTEM tokens may request any user ID. Requires the VIEW_TRANSACTIONS action.
    /// </remarks>
    [HttpPost]
    [EndpointSummary("Get positions")]
    [EndpointDescription("Returns the requested user's open positions, including current market value and unrealized gain or loss.")]
    [ProducesResponseType<PositionQueryResponse>(StatusCodes.Status200OK, Description = "The position snapshot was returned. Quote failures are reported as warnings while the request still succeeds.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, Description = "The position request failed validation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, Description = "The caller is not authorized to request positions for the specified user.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, Description = "The balances and positions service encountered an unexpected error.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, Description = "The balances and positions service could not reach a required dependency.")]
    public async Task<ActionResult<PositionQueryResponse>> Post(
        [FromBody] PositionQueryRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequestOwnership(request.UserId);
        ValidateView(request.View);

        return Ok(await _positionQueryService.GetPositionsAsync(request, cancellationToken));
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
                "The authenticated token cannot request positions for a different user.");
        }
    }

    private static void ValidateView(PositionView? view)
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
