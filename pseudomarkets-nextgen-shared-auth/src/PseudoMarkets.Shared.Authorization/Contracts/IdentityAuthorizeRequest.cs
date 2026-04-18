namespace PseudoMarkets.Shared.Authorization.Contracts;

public sealed record IdentityAuthorizeRequest(string Token, string Action);
