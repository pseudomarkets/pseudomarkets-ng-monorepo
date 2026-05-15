namespace PseudoMarkets.Security.IdentityServer.Core.Constants;

public static class DatabaseConstants
{
    public const string Namespace = "nsPseudoMarkets";
    public const string AccountsSet = "sAccounts";
    public const string UserIdsSet = "sUserIds";
    public const string UserIdBin = "bUserId";
    public const string LoginIdBin = "bLoginId";
    public const string HashedPasswordBin = "bPass";
    public const string HashedPasswordResetKeyBin = "bResetKey";
    public const string AccountTypeBin = "bType";
    public const string RolesBin = "bRoles";
    public const string ActiveBin = "bActive";
    public const string FailedLoginAttemptsBin = "bFailCnt";
    public const string LockoutUntilBin = "bLockoutTs";
    
    public const string TokensSet = "sTokens";
    public const string TokenHashBin = "bTokHash";
    public const string IssuedAtBin = "bIssuedTs";
    public const string ExpirationBin = "bExpireTs";
    public const string ConsumedBin = "bConsumed";
    public const string RevokedBin = "bRevoked";
}
