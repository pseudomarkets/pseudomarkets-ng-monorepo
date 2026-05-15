using Aerospike.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PseudoMarkets.Security.IdentityServer.Core.Configuration;
using PseudoMarkets.Security.IdentityServer.Core.Constants;
using PseudoMarkets.Security.IdentityServer.Core.Database.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Exceptions;
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Database.Implementations;

public class AccountRepository : IAccountRepository
{
    private readonly IAerospikeClient _aerospikeClient;
    private readonly ILogger<AccountRepository> _logger;
    private readonly Policy _readPolicy = new Policy()
    {
        sendKey = true
    };

    private readonly WritePolicy _writePolicyCreate = new WritePolicy()
    {
        sendKey = true,
        durableDelete = true,
        recordExistsAction = RecordExistsAction.CREATE_ONLY
    };

    private readonly WritePolicy _writePolicyUpdate = new WritePolicy()
    {
        sendKey = true,
        durableDelete = true,
        recordExistsAction = RecordExistsAction.UPDATE_ONLY
    };

    private readonly WritePolicy _writePolicyDelete = new WritePolicy()
    {
        durableDelete = true
    };
    
    public AccountRepository(AerospikeConfiguration aerospikeConfiguration, ILogger<AccountRepository> logger)
    {
        _logger = logger;

        try
        {
            _aerospikeClient = new AerospikeClient(aerospikeConfiguration.Host, aerospikeConfiguration.Port);
        }
        catch (AerospikeException ex)
        {
            _logger.LogError(ex, "Failed to create Aerospike client for identity storage.");
            throw new IdentityDependencyException("Unable to initialize the identity data store.", ex);
        }
    }

    [ActivatorUtilitiesConstructor]
    public AccountRepository(IAerospikeClient aerospikeClient, ILogger<AccountRepository> logger)
    {
        _aerospikeClient = aerospikeClient;
        _logger = logger;
    }
    
    public Account? GetAccount(string loginId)
    {
        try
        {
            var record = _aerospikeClient.Get(_readPolicy, new Key(DatabaseConstants.Namespace, DatabaseConstants.AccountsSet, loginId));
            if (record == null)
            {
                return null;
            }

            return MapFromRecord(loginId, record);
        }
        catch (AerospikeException ex)
        {
            _logger.LogError(ex, "Failed to read account {LoginId} from Aerospike.", loginId);
            throw new IdentityDependencyException("Unable to access the identity data store.", ex);
        }
    }

    public bool TryReserveUserId(long userId, string loginId)
    {
        try
        {
            var reservationBins = new[]
            {
                new Bin(DatabaseConstants.UserIdBin, userId),
                new Bin(DatabaseConstants.LoginIdBin, loginId)
            };

            _aerospikeClient.Put(
                _writePolicyCreate,
                new Key(DatabaseConstants.Namespace, DatabaseConstants.UserIdsSet, userId),
                reservationBins);

            return true;
        }
        catch (AerospikeException ex) when (ex.Result == ResultCode.KEY_EXISTS_ERROR)
        {
            _logger.LogDebug("User ID {UserId} is already reserved.", userId);
            return false;
        }
        catch (AerospikeException ex)
        {
            _logger.LogError(ex, "Failed to reserve user ID {UserId} for {LoginId}.", userId, loginId);
            throw new IdentityDependencyException("Unable to reserve a unique user ID.", ex);
        }
    }

    public void ReleaseReservedUserId(long userId)
    {
        try
        {
            _aerospikeClient.Delete(_writePolicyDelete, new Key(DatabaseConstants.Namespace, DatabaseConstants.UserIdsSet, userId));
        }
        catch (AerospikeException ex)
        {
            _logger.LogWarning(ex, "Failed to release reserved user ID {UserId}.", userId);
            throw new IdentityDependencyException("Unable to release the reserved user ID.", ex);
        }
    }

    public void UpdateAccount(Account account)
    {
        try
        {
            _aerospikeClient.Put(_writePolicyUpdate, new Key(DatabaseConstants.Namespace, DatabaseConstants.AccountsSet, account.LoginId), MapToBins(account).ToArray());
        }
        catch (AerospikeException ex)
        {
            _logger.LogError(ex, "Failed to update account {LoginId} in Aerospike.", account.LoginId);
            throw new IdentityDependencyException("Unable to update identity data.", ex);
        }
    }
    
    public void CreateAccount(Account account)
    {
        try
        {
            _aerospikeClient.Put(_writePolicyCreate, new Key(DatabaseConstants.Namespace, DatabaseConstants.AccountsSet, account.LoginId), MapToBins(account).ToArray());
        }
        catch (AerospikeException ex)
        {
            _logger.LogError(ex, "Failed to create account {LoginId} in Aerospike.", account.LoginId);
            throw new IdentityDependencyException("Unable to create identity data.", ex);
        }
    }

    public RefreshTokenRecord? GetRefreshToken(string tokenId)
    {
        try
        {
            var record = _aerospikeClient.Get(_readPolicy, new Key(DatabaseConstants.Namespace, DatabaseConstants.TokensSet, tokenId));
            if (record == null)
            {
                return null;
            }

            return MapRefreshTokenFromRecord(tokenId, record);
        }
        catch (AerospikeException ex)
        {
            _logger.LogError(ex, "Failed to read refresh token {TokenId} from Aerospike.", tokenId);
            throw new IdentityDependencyException("Unable to access refresh token data.", ex);
        }
    }

    public void CreateRefreshToken(RefreshTokenRecord refreshToken)
    {
        try
        {
            _aerospikeClient.Put(
                _writePolicyCreate,
                new Key(DatabaseConstants.Namespace, DatabaseConstants.TokensSet, refreshToken.TokenId),
                MapToBins(refreshToken).ToArray());
        }
        catch (AerospikeException ex)
        {
            _logger.LogError(ex, "Failed to create refresh token {TokenId} in Aerospike.", refreshToken.TokenId);
            throw new IdentityDependencyException("Unable to create refresh token data.", ex);
        }
    }

    public void UpdateRefreshToken(RefreshTokenRecord refreshToken)
    {
        try
        {
            _aerospikeClient.Put(
                _writePolicyUpdate,
                new Key(DatabaseConstants.Namespace, DatabaseConstants.TokensSet, refreshToken.TokenId),
                MapToBins(refreshToken).ToArray());
        }
        catch (AerospikeException ex)
        {
            _logger.LogError(ex, "Failed to update refresh token {TokenId} in Aerospike.", refreshToken.TokenId);
            throw new IdentityDependencyException("Unable to update refresh token data.", ex);
        }
    }

    public bool TryConsumeRefreshToken(string tokenId, int expectedGeneration)
    {
        try
        {
            var policy = new WritePolicy(_writePolicyUpdate)
            {
                generationPolicy = GenerationPolicy.EXPECT_GEN_EQUAL,
                generation = expectedGeneration
            };

            _aerospikeClient.Put(
                policy,
                new Key(DatabaseConstants.Namespace, DatabaseConstants.TokensSet, tokenId),
                new Bin(DatabaseConstants.ConsumedBin, true));

            return true;
        }
        catch (AerospikeException ex) when (ex.Result == ResultCode.GENERATION_ERROR)
        {
            _logger.LogInformation("Refresh token {TokenId} was already consumed or updated.", tokenId);
            return false;
        }
        catch (AerospikeException ex)
        {
            _logger.LogError(ex, "Failed to consume refresh token {TokenId} in Aerospike.", tokenId);
            throw new IdentityDependencyException("Unable to consume refresh token data.", ex);
        }
    }

    private List<Bin> MapToBins(Account account)
    {
        var userId = new Bin(DatabaseConstants.UserIdBin, account.UserId);
        var hashedPassword = new Bin(DatabaseConstants.HashedPasswordBin, account.HashedPassword);
        var hashedPasswordResetKey = new Bin(DatabaseConstants.HashedPasswordResetKeyBin, account.HashedPasswordResetKey);
        var roles = new Bin(DatabaseConstants.RolesBin, account.Roles);
        var accountType = new Bin(DatabaseConstants.AccountTypeBin, account.AccountType);
        var activeBin = new Bin(DatabaseConstants.ActiveBin, account.IsActive);
        var failedLoginAttemptsBin = new Bin(DatabaseConstants.FailedLoginAttemptsBin, account.FailedLoginAttempts);
        var lockoutUntilBin = new Bin(
            DatabaseConstants.LockoutUntilBin,
            account.LockoutUntilUtc?.Ticks ?? 0L);
        
        var bins = new List<Bin>()
        {
            userId,
            activeBin,
            roles,
            accountType,
            hashedPassword,
            hashedPasswordResetKey,
            failedLoginAttemptsBin,
            lockoutUntilBin
        };
        
        return bins;
    }

    private List<Bin> MapToBins(RefreshTokenRecord refreshToken)
    {
        return
        [
            new Bin(DatabaseConstants.TokenHashBin, refreshToken.SecretHash),
            new Bin(DatabaseConstants.LoginIdBin, refreshToken.LoginId),
            new Bin(DatabaseConstants.UserIdBin, refreshToken.UserId),
            new Bin(DatabaseConstants.AccountTypeBin, refreshToken.AccountType),
            new Bin(DatabaseConstants.IssuedAtBin, refreshToken.IssuedAtUtc.Ticks),
            new Bin(DatabaseConstants.ExpirationBin, refreshToken.ExpiresAtUtc.Ticks),
            new Bin(DatabaseConstants.ConsumedBin, refreshToken.IsConsumed),
            new Bin(DatabaseConstants.RevokedBin, refreshToken.IsRevoked)
        ];
    }

    private Account MapFromRecord(string loginId, Record record)
    {
        var userId = record.GetLong(DatabaseConstants.UserIdBin);
        var hashedPassword = record.GetString(DatabaseConstants.HashedPasswordBin) ?? string.Empty;
        var hashedPasswordResetKey = record.GetString(DatabaseConstants.HashedPasswordResetKeyBin) ?? string.Empty;
        var accountType = record.GetString(DatabaseConstants.AccountTypeBin) ?? string.Empty;
        var isActive = record.GetBool(DatabaseConstants.ActiveBin);
        var failedLoginAttempts = (int)record.GetLong(DatabaseConstants.FailedLoginAttemptsBin);
        var lockoutUntilTicks = record.GetLong(DatabaseConstants.LockoutUntilBin);
        var rawRoles = record.GetList(DatabaseConstants.RolesBin)?.Cast<object?>() ?? Array.Empty<object?>();

        var roles = new List<string>();
        foreach (var role in rawRoles)
        {
            var roleAsString = Convert.ToString(role);
            if (!string.IsNullOrWhiteSpace(roleAsString))
            {
                roles.Add(roleAsString);
            }
        }
        
        return new Account()
        {
            LoginId = loginId,
            UserId = userId,
            HashedPassword = hashedPassword,
            HashedPasswordResetKey = hashedPasswordResetKey,
            AccountType = accountType,
            IsActive = isActive,
            Roles = roles,
            FailedLoginAttempts = failedLoginAttempts,
            LockoutUntilUtc = lockoutUntilTicks > 0 ? new DateTime(lockoutUntilTicks, DateTimeKind.Utc) : null
        };
    }

    private static RefreshTokenRecord MapRefreshTokenFromRecord(string tokenId, Record record)
    {
        return new RefreshTokenRecord
        {
            TokenId = tokenId,
            SecretHash = record.GetString(DatabaseConstants.TokenHashBin) ?? string.Empty,
            LoginId = record.GetString(DatabaseConstants.LoginIdBin) ?? string.Empty,
            UserId = record.GetLong(DatabaseConstants.UserIdBin),
            AccountType = record.GetString(DatabaseConstants.AccountTypeBin) ?? string.Empty,
            IssuedAtUtc = new DateTime(record.GetLong(DatabaseConstants.IssuedAtBin), DateTimeKind.Utc),
            ExpiresAtUtc = new DateTime(record.GetLong(DatabaseConstants.ExpirationBin), DateTimeKind.Utc),
            IsConsumed = record.GetBool(DatabaseConstants.ConsumedBin),
            IsRevoked = record.GetBool(DatabaseConstants.RevokedBin),
            Generation = record.generation
        };
    }
}
