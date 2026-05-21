using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.OrderExecution.Contracts.Orders;
using PseudoMarkets.OrderExecution.Core.Interfaces;
using PseudoMarkets.OrderExecution.Core.Models;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;
using PseudoMarkets.Shared.Authorization.Models;

namespace PseudoMarkets.OrderExecution.Service.Controllers;

[ApiController]
[Route("api/orders")]
[AuthorizeWithIdentityServer(PlatformAuthorizationActions.ExecuteTrades)]
[Produces("application/json")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderSubmissionService _orderSubmissionService;

    public OrdersController(IOrderSubmissionService orderSubmissionService)
    {
        _orderSubmissionService = orderSubmissionService;
    }

    /// <summary>
    /// Submits an order for execution or queueing.
    /// </summary>
    /// <remarks>
    /// Accepts a market order for a platform-supported trading instrument. Orders submitted during market hours are
    /// executed immediately; valid off-hours orders are queued for later batch processing. Requires the EXECUTE_TRADES action.
    /// </remarks>
    [HttpPost]
    [EndpointSummary("Submit order")]
    [EndpointDescription("Submits a market order for immediate execution during market hours or queueing outside market hours.")]
    [ProducesResponseType<SubmitOrderResponse>(StatusCodes.Status200OK, Description = "The order was accepted, filled, queued, or rejected by business validation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, Description = "The order request failed validation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, Description = "The token is not authorized to execute trades.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, Description = "The order execution service encountered an unexpected error.")]
    public async Task<ActionResult<SubmitOrderResponse>> Submit(
        [FromBody] SubmitOrderRequest request,
        CancellationToken cancellationToken)
    {
        var callerContext = new OrderCallerContext(GetAuthorizedUserId(), GetAuthorizedTokenType());
        return Ok(await _orderSubmissionService.SubmitAsync(request, callerContext, cancellationToken));
    }

    private long GetAuthorizedUserId()
    {
        if (HttpContext.Items.TryGetValue(AuthorizedIdentityContext.UserIdItemKey, out var value) &&
            value is long userId)
        {
            return userId;
        }

        return 0;
    }

    private string GetAuthorizedTokenType()
    {
        if (HttpContext.Items.TryGetValue(AuthorizedIdentityContext.TokenTypeItemKey, out var value) &&
            value is string tokenType)
        {
            return tokenType;
        }

        return string.Empty;
    }
}
