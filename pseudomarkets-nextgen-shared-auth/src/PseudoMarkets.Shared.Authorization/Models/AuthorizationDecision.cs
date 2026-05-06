using Microsoft.AspNetCore.Http;

namespace PseudoMarkets.Shared.Authorization.Models;

public sealed record AuthorizationDecision(
    bool IsAuthorized,
    int StatusCode,
    string Title,
    string Detail,
    long? UserId = null,
    string TokenType = "")
{
    public static AuthorizationDecision Authorized()
    {
        return new AuthorizationDecision(true, StatusCodes.Status200OK, string.Empty, string.Empty);
    }

    public static AuthorizationDecision Authorized(long userId, string tokenType)
    {
        return new AuthorizationDecision(true, StatusCodes.Status200OK, string.Empty, string.Empty, userId, tokenType);
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
