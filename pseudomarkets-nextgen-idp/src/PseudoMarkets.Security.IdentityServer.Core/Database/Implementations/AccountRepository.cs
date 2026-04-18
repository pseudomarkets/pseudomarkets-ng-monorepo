using Aerospike.Client;
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

    private List<Bin> MapToBins(Account account)
    {
        var userId = new Bin(DatabaseConstants.UserIdBin, account.UserId);
        var hashedPassword = new Bin(DatabaseConstants.HashedPasswordBin, account.HashedPassword);
        var roles = new Bin(DatabaseConstants.RolesBin, account.Roles);
        var accountType = new Bin(DatabaseConstants.AccountTypeBin, account.AccountType);
        var activeBin = new Bin(DatabaseConstants.ActiveBin, account.IsActive);
        
        var bins = new List<Bin>()
        {
            userId, activeBin, roles, accountType, hashedPassword
        };
        
        return bins;
    }

    private Account MapFromRecord(string loginId, Record record)
    {
        var userId = record.GetLong(DatabaseConstants.UserIdBin);
        var hashedPassword = record.GetString(DatabaseConstants.HashedPasswordBin) ?? string.Empty;
        var accountType = record.GetString(DatabaseConstants.AccountTypeBin) ?? string.Empty;
        var isActive = record.GetBool(DatabaseConstants.ActiveBin);
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
            AccountType = accountType,
            IsActive = isActive,
            Roles = roles
        };
    }
}
