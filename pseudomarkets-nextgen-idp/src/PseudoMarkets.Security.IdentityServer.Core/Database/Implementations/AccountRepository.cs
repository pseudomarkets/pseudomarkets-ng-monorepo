using Aerospike.Client;
using PseudoMarkets.Security.IdentityServer.Core.Configuration;
using PseudoMarkets.Security.IdentityServer.Core.Constants;
using PseudoMarkets.Security.IdentityServer.Core.Database.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Database.Implementations;

public class AccountRepository : IAccountRepository
{
    private readonly IAerospikeClient _aerospikeClient;
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
    
    public AccountRepository(AerospikeConfiguration aerospikeConfiguration)
    {
        _aerospikeClient = new AerospikeClient(aerospikeConfiguration.Host, aerospikeConfiguration.Port);
    }
    
    public Account? GetAccount(string loginId)
    {
        var record = _aerospikeClient.Get(_readPolicy, new Key(DatabaseConstants.Namespace, DatabaseConstants.AccountsSet, loginId));
        if(record == null)
            return null;
        
        return MapFromRecord(record);
    }

    public void UpdateAccount(Account account)
    {
        _aerospikeClient.Put(_writePolicyUpdate, new Key(DatabaseConstants.Namespace, DatabaseConstants.AccountsSet, account.LoginId), MapToBins(account).ToArray());
    }
    
    public void CreateAccount(Account account)
    {
        _aerospikeClient.Put(_writePolicyCreate, new Key(DatabaseConstants.Namespace, DatabaseConstants.AccountsSet, account.LoginId), MapToBins(account).ToArray());
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

    private Account MapFromRecord(Record record)
    {
        var userId = record.GetLong(DatabaseConstants.UserIdBin);
        var hashedPassword = record.GetString(DatabaseConstants.HashedPasswordBin);
        var accountType = record.GetString(DatabaseConstants.AccountTypeBin);
        var isActive = record.GetBool(DatabaseConstants.ActiveBin);
        var rawRoles = record.GetList(DatabaseConstants.RolesBin);

        var roles = new List<string>();
        foreach (var role in rawRoles)
        {
            roles.Add(Convert.ToString(role));
        }
        
        return new Account()
        {
            UserId = userId,
            HashedPassword = hashedPassword,
            AccountType = accountType,
            IsActive = isActive,
            Roles = roles
        };
    }
}