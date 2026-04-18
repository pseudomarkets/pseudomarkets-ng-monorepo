using Microsoft.AspNetCore.Http;

namespace PseudoMarkets.Shared.Authorization.Models;

public sealed record AuthorizationDecision(bool IsAuthorized, int StatusCode, string Title, string Detail)
{
    public static AuthorizationDecision Authorized()
    {
        return new AuthorizationDecision(true, StatusCodes.Status200OK, string.Empty, string.Empty);
    }

    public static AuthorizationDecision Unauthorized(string detail)
    {
        return new AuthorizationDecision(false, StatusCodes.Status401Unauthorized, "Authorization required", detail);
    }

    public static AuthorizationDecision Forbidden(string detail)
    {
        return new AuthorizationDecision(false, StatusCodes.Status403Forbidden, "Forbidden", detail);
    }

    public static AuthorizationDecision DependencyFailure(string detail)
    {
        return new AuthorizationDecision(false, StatusCodes.Status503ServiceUnavailable, "Authorization unavailable", detail);
    }
}
