namespace PseudoMarkets.Security.IdentityServer.Core.Models;

/// <summary>
/// Response returned after authentication or refresh-token rotation.
/// </summary>
public class AuthenticationResult
{
    /// <summary>
    /// Indicates whether authentication or refresh succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// JWT access token issued by the IDP.
    /// </summary>
    public string Token { get; }

    /// <summary>
    /// UTC expiration time for the JWT access token.
    /// </summary>
    public DateTime Expires { get; }

    /// <summary>
    /// Opaque refresh token that can be used once to request replacement token material.
    /// </summary>
    public string RefreshToken { get; }

    /// <summary>
    /// UTC expiration time for the refresh token.
    /// </summary>
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
