using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using PseudoMarkets.Security.IdentityServer.Core.Accounts.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Authentication.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Constants;
using PseudoMarkets.Security.IdentityServer.Core.Database.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Exceptions;
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Accounts.Implementations;

public class AccountProvisioningManager : IAccountProvisioningManager
{
    private const int MaxUserIdReservationAttempts = 25;
    private readonly IAccountRepository _accountRepository;
    private readonly IAuthenticationManager _authenticationManager;
    private readonly ILogger<AccountProvisioningManager> _logger;

    public AccountProvisioningManager(
        IAccountRepository accountRepository,
        IAuthenticationManager authenticationManager,
        ILogger<AccountProvisioningManager> logger)
    {
        _accountRepository = accountRepository;
        _authenticationManager = authenticationManager;
        _logger = logger;
    }

    public AccountCreationResult CreateAccount(string loginId, string password, string accountType)
    {
        var normalizedAccountType = NormalizeAccountType(accountType);
        if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(password))
        {
            return new AccountCreationResult(false, "Username and password are required.", loginId, normalizedAccountType);
        }

        if (!IsSupportedAccountType(normalizedAccountType))
        {
            return new AccountCreationResult(false, "Unsupported account type.", loginId, normalizedAccountType);
        }

        try
        {
            var existingAccount = _accountRepository.GetAccount(loginId);
            if (existingAccount != null)
            {
                return new AccountCreationResult(false, "An account with that username already exists.", loginId, existingAccount.AccountType);
            }

            var userId = ReserveUniqueUserId(loginId);

            var account = new Account
            {
                LoginId = loginId,
                UserId = userId,
                HashedPassword = _authenticationManager.HashPassword(password),
                AccountType = normalizedAccountType,
                Roles = GetDefaultRoles(normalizedAccountType),
                IsActive = true
            };

            try
            {
                _accountRepository.CreateAccount(account);
            }
            catch
            {
                TryReleaseReservedUserId(userId);
                throw;
            }

            return new AccountCreationResult(
                true,
                $"{normalizedAccountType} account created successfully.",
                account.LoginId,
                account.AccountType);
        }
        catch (IdentityDependencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating account {LoginId} with type {AccountType}.", loginId, normalizedAccountType);
            throw new IdentityServiceException("Unable to create the account.", ex);
        }
    }

    private static string NormalizeAccountType(string? accountType)
    {
        if (string.IsNullOrWhiteSpace(accountType))
        {
            return AccountTypeConstants.UserType;
        }

        return accountType.Trim().ToUpperInvariant();
    }

    private static bool IsSupportedAccountType(string accountType)
    {
        return accountType is AccountTypeConstants.UserType or AccountTypeConstants.SystemType;
    }

    private static List<string> GetDefaultRoles(string accountType)
    {
        return accountType == AccountTypeConstants.SystemType
            ? [.. RoleConstants.AllRoles]
            : [.. RoleConstants.NonSystemUserRoles];
    }

    private long ReserveUniqueUserId(string loginId)
    {
        for (var attempt = 0; attempt < MaxUserIdReservationAttempts; attempt++)
        {
            var userId = GenerateUserId();
            if (_accountRepository.TryReserveUserId(userId, loginId))
            {
                return userId;
            }
        }

        throw new IdentityServiceException("Unable to allocate a unique user ID for the account.");
    }

    private void TryReleaseReservedUserId(long userId)
    {
        try
        {
            _accountRepository.ReleaseReservedUserId(userId);
        }
        catch (IdentityDependencyException ex)
        {
            _logger.LogWarning(ex, "Failed to clean up reserved user ID {UserId} after account creation failure.", userId);
        }
    }

    private static long GenerateUserId()
    {
        const long minUserId = 1_000_000_000;
        const long maxUserId = 9_999_999_999;
        const ulong range = (ulong)(maxUserId - minUserId + 1);
        var limit = ulong.MaxValue - (ulong.MaxValue % range);

        while (true)
        {
            var userIdBytes = RandomNumberGenerator.GetBytes(sizeof(ulong));
            var candidate = BitConverter.ToUInt64(userIdBytes, 0);
            if (candidate < limit)
            {
                return (long)(candidate % range) + minUserId;
            }
        }
    }
}
