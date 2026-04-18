namespace PseudoMarkets.Shared.Authorization.Configuration;

public class IdentityAuthorizationConfiguration
{
    public const string SectionName = "IdentityAuthorization";

    public string IdentityServerBaseUrl { get; set; } = string.Empty;
    public string AuthorizeEndpointPath { get; set; } = "/api/identity/authorize";
    public int TimeoutSeconds { get; set; } = 10;
}
