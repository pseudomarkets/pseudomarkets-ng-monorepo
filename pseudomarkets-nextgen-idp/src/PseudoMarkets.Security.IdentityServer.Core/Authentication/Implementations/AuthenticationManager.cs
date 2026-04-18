using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.IdentityModel.Tokens;
using PseudoMarkets.Security.IdentityServer.Core.Authentication.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Configuration;
using PseudoMarkets.Security.IdentityServer.Core.Database.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Authentication.Implementations;

public class AuthenticationManager : IAuthenticationManager
{
    private readonly JwtConfiguration _jwtConfiguration;
    private readonly IAccountRepository _accountRepository;

    public AuthenticationManager(JwtConfiguration jwtConfiguration, IAccountRepository accountRepository)
    {
        _jwtConfiguration = jwtConfiguration;
        _accountRepository = accountRepository;
    }
    
    private string GenerateToken(string loginId, int expiresIn)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, loginId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public AuthenticationResult Authenticate(string loginId, string password)
    {
        var account = _accountRepository.GetAccount(loginId);
        if (account != null && account.HashedPassword == HashPassword(password))
        {
            return new AuthenticationResult(true, GenerateToken(loginId, 60), DateTime.UtcNow.AddMinutes(60));
        }

        return new AuthenticationResult(false, string.Empty, DateTime.MinValue);
    }

    public string HashPassword(string plainTextPassword)
    {
        var salt = RandomNumberGenerator.GetBytes(128 / 8); 
        
        return Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: plainTextPassword,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));
    }
}