using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PseudoMarkets.Security.IdentityServer.Core.Authentication.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Configuration;
using PseudoMarkets.Security.IdentityServer.Core.Database.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Exceptions;
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Authentication.Implementations;

public class AuthenticationManager : IAuthenticationManager
{
    private readonly JwtConfiguration _jwtConfiguration;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<AuthenticationManager> _logger;

    public AuthenticationManager(
        JwtConfiguration jwtConfiguration,
        IAccountRepository accountRepository,
        ILogger<AuthenticationManager> logger)
    {
        _jwtConfiguration = jwtConfiguration;
        _accountRepository = accountRepository;
        _logger = logger;
    }
    
    private string GenerateToken(string loginId, int expiresIn, string roles)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, loginId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("roles",  roles)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfiguration.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(expiresIn));

        var token = new JwtSecurityToken(
            issuer: _jwtConfiguration.Issuer,
            audience: _jwtConfiguration.Audience,
            claims: claims,
            expires: expires,
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
            if (account != null && VerifyPassword(account.HashedPassword, password))
            {
                var roles = string.Join(",", account.Roles);
                return new AuthenticationResult(true, GenerateToken(loginId, 60, roles), DateTime.UtcNow.AddMinutes(60));
            }

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
}
