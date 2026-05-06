namespace PseudoMarkets.Security.IdentityServer.Core.Models;

public class AuthenticationResult
{
    public bool Success { get; }
    public string Token { get; }
    public DateTime Expires { get; }
    public string RefreshToken { get; }
    public DateTime RefreshTokenExpires { get; }

    public AuthenticationResult(
        bool success,
        string token,
        DateTime expires,
        string refreshToken = "",
        DateTime? refreshTokenExpires = null)
    {
        Success = success;
        Token = token;
        Expires = expires;
        RefreshToken = refreshToken;
        RefreshTokenExpires = refreshTokenExpires ?? DateTime.MinValue;
    }
}
