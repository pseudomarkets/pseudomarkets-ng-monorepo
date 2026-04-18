namespace PseudoMarkets.Shared.Authorization.Contracts;

public sealed class IdentityAuthorizationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
