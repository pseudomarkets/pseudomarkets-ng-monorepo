namespace PseudoMarkets.Security.IdentityServer.Core.Configuration;

public sealed class IdentitySecurityConfiguration
{
    public const string SectionName = "IdentitySecurity";

    public string SystemAccountBypassKey { get; init; } = string.Empty;
    public int FailedLoginAttemptLimit { get; init; } = 5;
    public int LockoutDurationMinutes { get; init; } = 15;
    public int SensitiveEndpointPermitLimit { get; init; } = 10;
    public int SensitiveEndpointWindowMinutes { get; init; } = 1;
}
