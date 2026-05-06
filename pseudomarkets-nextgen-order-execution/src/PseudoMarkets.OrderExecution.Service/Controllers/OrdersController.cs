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
public sealed class OrdersController : ControllerBase
{
    private const string BearerPrefix = "Bearer ";
    private readonly IOrderSubmissionService _orderSubmissionService;

    public OrdersController(IOrderSubmissionService orderSubmissionService)
    {
        _orderSubmissionService = orderSubmissionService;
    }

    [HttpPost]
    public async Task<ActionResult<SubmitOrderResponse>> Submit(
        [FromBody] SubmitOrderRequest request,
        CancellationToken cancellationToken)
    {
        var callerContext = new OrderCallerContext(GetAuthorizedUserId(), GetBearerToken());
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

    private string GetBearerToken()
    {
        var authorizationHeader = Request.Headers.Authorization.ToString();
        if (authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return authorizationHeader[BearerPrefix.Length..].Trim();
        }

        return string.Empty;
    }
}
