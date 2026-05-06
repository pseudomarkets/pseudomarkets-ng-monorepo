using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PseudoMarkets.Security.IdentityServer.Core.Authentication.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Configuration;
using PseudoMarkets.Security.IdentityServer.Core.Database.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Exceptions;
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Authentication.Implementations;

public class AuthenticationManager : IAuthenticationManager
{
    private const int AccessTokenLifetimeMinutes = 60;
    private const int RefreshTokenLifetimeMinutes = 60;

    private readonly JwtConfiguration _jwtConfiguration;
    private readonly IdentitySecurityConfiguration _securityConfiguration;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<AuthenticationManager> _logger;

    public AuthenticationManager(
        JwtConfiguration jwtConfiguration,
        IOptions<IdentitySecurityConfiguration> securityConfiguration,
        IAccountRepository accountRepository,
        ILogger<AuthenticationManager> logger)
    {
        _jwtConfiguration = jwtConfiguration;
        _securityConfiguration = securityConfiguration.Value;
        _accountRepository = accountRepository;
        _logger = logger;
    }

    private string GenerateToken(Account account, DateTime expiresAtUtc)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.LoginId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("roles", string.Join(",", account.Roles)),
            new Claim("id", account.UserId.ToString()),
            new Claim("account_type", account.AccountType)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfiguration.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtConfiguration.Issuer,
            audience: _jwtConfiguration.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: creds
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.OutboundClaimTypeMap.Clear();
        return tokenHandler.WriteToken(token);
    }

    public AuthenticationResult Authenticate(string loginId, string password)
    {
        if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(password))
        {
            return new AuthenticationResult(false, string.Empty, DateTime.MinValue);
        }

        try
        {
            var account = _accountRepository.GetAccount(loginId);
            if (account is not null && IsAccountLockedOut(account))
            {
                return new AuthenticationResult(false, string.Empty, DateTime.MinValue);
            }

            if (account != null && account.IsActive && VerifyPassword(account.HashedPassword, password))
            {
                ResetFailedLoginStateIfNeeded(account);
                return CreateAuthenticationResult(account);
            }

            RegisterFailedLoginAttempt(account);
            return new AuthenticationResult(false, string.Empty, DateTime.MinValue);
        }
        catch (IdentityDependencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while authenticating {LoginId}.", loginId);
            throw new IdentityServiceException("Unable to complete authentication.", ex);
        }
    }

    public AuthenticationResult Refresh(string refreshToken)
    {
        if (!TryParseRefreshToken(refreshToken, out var tokenId, out var tokenSecret))
        {
            return new AuthenticationResult(false, string.Empty, DateTime.MinValue);
        }

        try
        {
            var storedToken = _accountRepository.GetRefreshToken(tokenId);
            if (storedToken is null || !IsRefreshTokenUsable(storedToken, tokenSecret))
            {
                return new AuthenticationResult(false, string.Empty, DateTime.MinValue);
            }

            var account = _accountRepository.GetAccount(storedToken.LoginId);
            if (account is null || !account.IsActive)
            {
                return new AuthenticationResult(false, string.Empty, DateTime.MinValue);
            }

            if (!_accountRepository.TryConsumeRefreshToken(storedToken.TokenId, storedToken.Generation))
            {
                return new AuthenticationResult(false, string.Empty, DateTime.MinValue);
            }

            return CreateAuthenticationResult(account);
        }
        catch (IdentityDependencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while refreshing an identity token.");
            throw new IdentityServiceException("Unable to complete token refresh.", ex);
        }
    }

    public string HashPassword(string plainTextPassword)
    {
        var salt = RandomNumberGenerator.GetBytes(128 / 8);
        var hashBytes = KeyDerivation.Pbkdf2(
            password: plainTextPassword,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hashBytes)}";
    }

    private AuthenticationResult CreateAuthenticationResult(Account account)
    {
        var accessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(AccessTokenLifetimeMinutes);
        var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(RefreshTokenLifetimeMinutes);
        var issuedRefreshToken = IssueRefreshToken(account, refreshTokenExpiresAtUtc);

        return new AuthenticationResult(
            true,
            GenerateToken(account, accessTokenExpiresAtUtc),
            accessTokenExpiresAtUtc,
            issuedRefreshToken.PlainTextToken,
            issuedRefreshToken.ExpiresAtUtc);
    }

    private IssuedRefreshToken IssueRefreshToken(Account account, DateTime refreshTokenExpiresAtUtc)
    {
        var tokenId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var tokenSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var issuedAtUtc = DateTime.UtcNow;
        var storedToken = new RefreshTokenRecord
        {
            TokenId = tokenId,
            SecretHash = HashRefreshTokenSecret(tokenSecret),
            LoginId = account.LoginId,
            UserId = account.UserId,
            AccountType = account.AccountType,
            IssuedAtUtc = issuedAtUtc,
            ExpiresAtUtc = refreshTokenExpiresAtUtc,
            IsConsumed = false,
            IsRevoked = false
        };

        _accountRepository.CreateRefreshToken(storedToken);
        return new IssuedRefreshToken($"{tokenId}.{tokenSecret}", refreshTokenExpiresAtUtc);
    }

    private static bool TryParseRefreshToken(string refreshToken, out string tokenId, out string tokenSecret)
    {
        tokenId = string.Empty;
        tokenSecret = string.Empty;

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        var parts = refreshToken.Split('.', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            return false;
        }

        tokenId = parts[0];
        tokenSecret = parts[1];
        return true;
    }

    private static string HashRefreshTokenSecret(string secret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        return Convert.ToBase64String(SHA256.HashData(secretBytes));
    }

    private static bool IsRefreshTokenUsable(RefreshTokenRecord storedToken, string tokenSecret)
    {
        if (storedToken.IsConsumed || storedToken.IsRevoked || storedToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return false;
        }

        try
        {
            var expectedHash = Convert.FromBase64String(storedToken.SecretHash);
            var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(tokenSecret));
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private bool IsAccountLockedOut(Account account)
    {
        return account.LockoutUntilUtc.HasValue && account.LockoutUntilUtc.Value > DateTime.UtcNow;
    }

    private void RegisterFailedLoginAttempt(Account? account)
    {
        if (account is null)
        {
            return;
        }

        account.FailedLoginAttempts++;
        if (account.FailedLoginAttempts >= _securityConfiguration.FailedLoginAttemptLimit)
        {
            account.LockoutUntilUtc = DateTime.UtcNow.AddMinutes(
                _securityConfiguration.LockoutDurationMinutes > 0
                    ? _securityConfiguration.LockoutDurationMinutes
                    : 15);
            account.FailedLoginAttempts = 0;
        }

        _accountRepository.UpdateAccount(account);
    }

    private void ResetFailedLoginStateIfNeeded(Account account)
    {
        if (account.FailedLoginAttempts == 0 && account.LockoutUntilUtc is null)
        {
            return;
        }

        account.FailedLoginAttempts = 0;
        account.LockoutUntilUtc = null;
        _accountRepository.UpdateAccount(account);
    }

    private bool VerifyPassword(string hashedPassword, string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(plainTextPassword))
        {
            return false;
        }

        try
        {
            var hashParts = hashedPassword.Split(':', 2);
            if (hashParts.Length != 2)
            {
                return false;
            }

            var salt = Convert.FromBase64String(hashParts[0]);
            var expectedHash = Convert.FromBase64String(hashParts[1]);
            var actualHash = KeyDerivation.Pbkdf2(
                password: plainTextPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Stored password hash format is invalid.");
            return false;
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(ex, "Password hash verification failed due to an invalid cryptographic payload.");
            return false;
        }
    }

    private sealed record IssuedRefreshToken(string PlainTextToken, DateTime ExpiresAtUtc);
}
