using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using PseudoMarkets.Shared.Authorization.Interfaces;

namespace PseudoMarkets.Shared.Authorization.Filters;

public class RequireIdentityActionFilter : IAsyncAuthorizationFilter
{
    private const string BearerPrefix = "Bearer ";

    private readonly string _requiredAction;
    private readonly IIdentityAuthorizationClient _identityAuthorizationClient;
    private readonly ILogger<RequireIdentityActionFilter> _logger;

    public RequireIdentityActionFilter(
        string requiredAction,
        IIdentityAuthorizationClient identityAuthorizationClient,
        ILogger<RequireIdentityActionFilter> logger)
    {
        _requiredAction = requiredAction;
        _identityAuthorizationClient = identityAuthorizationClient;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            return;
        }

        if (!TryGetBearerToken(context.HttpContext.Request, out var token))
        {
            context.Result = CreateProblemResult(StatusCodes.Status401Unauthorized, "Authorization required", "A valid Bearer token is required.");
            return;
        }

        var decision = await _identityAuthorizationClient.AuthorizeAsync(
            token,
            _requiredAction,
            context.HttpContext.RequestAborted);

        if (decision.IsAuthorized)
        {
            return;
        }

        _logger.LogDebug(
            "Authorization failed for action {Action} with status code {StatusCode}.",
            _requiredAction,
            decision.StatusCode);

        context.Result = CreateProblemResult(decision.StatusCode, decision.Title, decision.Detail);
    }

    private static bool TryGetBearerToken(HttpRequest request, out string token)
    {
        token = string.Empty;

        if (!request.Headers.TryGetValue("Authorization", out var authorizationHeaderValues))
        {
            return false;
        }

        var authorizationHeader = authorizationHeaderValues.ToString();
        if (!authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = authorizationHeader[BearerPrefix.Length..].Trim();
        return !string.IsNullOrWhiteSpace(token);
    }

    private static ObjectResult CreateProblemResult(int statusCode, string title, string detail)
    {
        return new ObjectResult(new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode
        })
        {
            StatusCode = statusCode
        };
    }
}
