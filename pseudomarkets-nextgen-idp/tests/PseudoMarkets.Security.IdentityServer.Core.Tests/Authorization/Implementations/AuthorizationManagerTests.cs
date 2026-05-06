using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.Security.IdentityServer.Core.Authorization.Implementations;
using PseudoMarkets.Security.IdentityServer.Core.Configuration;
using PseudoMarkets.Security.IdentityServer.Core.Database.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Tests.Authorization.Implementations;

[TestFixture]
public class AuthorizationManagerTests
{
    private JwtConfiguration _jwtConfiguration = null!;
    private Mock<IAccountRepository> _accountRepository = null!;
    private AuthorizationManager _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _jwtConfiguration = new JwtConfiguration
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Key = "test-signing-key-1234567890-abcdef"
        };
        _accountRepository = new Mock<IAccountRepository>();
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account
        {
            LoginId = "user",
            UserId = 1_000_000_000L,
            AccountType = "SYSTEM",
            Roles = ["VIEW_BALANCES", "UPDATE_BALANCES"],
            IsActive = true
        });

        _sut = new AuthorizationManager(_jwtConfiguration, _accountRepository.Object, Mock.Of<ILogger<AuthorizationManager>>());
    }

    [TestCase("", "VIEW_BALANCES")]
    [TestCase("token", "")]
    public void Authorize_ShouldReturnFailure_WhenInputIsBlank(string token, string action)
    {
        var result = _sut.Authorize(new AuthorizationRequest(token, action));

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Authorization Failed");
    }

    [Test]
    public void Authorize_ShouldReturnSuccess_WhenRolesClaimContainsAction()
    {
        var token = CreateToken("roles", "VIEW_BALANCES,UPDATE_BALANCES", "SYSTEM");

        var result = _sut.Authorize(new AuthorizationRequest(token, "VIEW_BALANCES"));

        result.Success.ShouldBeTrue();
        result.Message.ShouldBe("Authorization Successful");
        result.TokenType.ShouldBe("SYSTEM");
    }

    [Test]
    public void Authorize_ShouldReturnUnauthorized_WhenRolesClaimDoesNotContainAction()
    {
        var token = CreateToken("roles", "UPDATE_BALANCES", "USER");
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account
        {
            LoginId = "user",
            UserId = 1_000_000_000L,
            AccountType = "USER",
            Roles = ["UPDATE_BALANCES"],
            IsActive = true
        });

        var result = _sut.Authorize(new AuthorizationRequest(token, "VIEW_BALANCES"));

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Unauthorized");
        result.TokenType.ShouldBe("USER");
    }

    [Test]
    public void Authorize_ShouldReturnFailure_WhenAccountIsInactive()
    {
        var token = CreateToken("roles", "VIEW_BALANCES", "SYSTEM");
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account
        {
            LoginId = "user",
            UserId = 1_000_000_000L,
            AccountType = "SYSTEM",
            Roles = ["VIEW_BALANCES"],
            IsActive = false
        });

        var result = _sut.Authorize(new AuthorizationRequest(token, "VIEW_BALANCES"));

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Authorization Failed");
    }

    [Test]
    public void Authorize_ShouldReturnFailure_WhenCurrentAccountRolesDoNotContainAction()
    {
        var token = CreateToken("roles", "VIEW_BALANCES", "SYSTEM");
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account
        {
            LoginId = "user",
            UserId = 1_000_000_000L,
            AccountType = "SYSTEM",
            Roles = ["UPDATE_BALANCES"],
            IsActive = true
        });

        var result = _sut.Authorize(new AuthorizationRequest(token, "VIEW_BALANCES"));

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Unauthorized");
    }

    [Test]
    public void Authorize_ShouldReturnFailure_WhenTokenIsInvalid()
    {
        var result = _sut.Authorize(new AuthorizationRequest("not-a-token", "VIEW_BALANCES"));

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Authorization Failed");
    }

    [Test]
    public void Authorize_ShouldReturnFailure_WhenLegacyRoleUriClaimIsUsed()
    {
        var token = CreateToken(ClaimTypes.Role, "VIEW_BALANCES", "USER");

        var result = _sut.Authorize(new AuthorizationRequest(token, "VIEW_BALANCES"));

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Authorization Failed");
    }

    [Test]
    public void Authorize_ShouldReturnFailure_WhenAccountTypeClaimIsMissing()
    {
        var token = CreateToken("roles", "VIEW_BALANCES", accountType: null);

        var result = _sut.Authorize(new AuthorizationRequest(token, "VIEW_BALANCES"));

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Authorization Failed");
    }

    private string CreateToken(string claimType, string claimValue, string? accountType)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, "user"),
            new Claim("id", "1000000000"),
            new Claim(claimType, claimValue)
        };

        if (!string.IsNullOrWhiteSpace(accountType))
        {
            claims.Add(new Claim("account_type", accountType));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfiguration.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtConfiguration.Issuer,
            audience: _jwtConfiguration.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds);

        var handler = new JwtSecurityTokenHandler();
        handler.OutboundClaimTypeMap.Clear();
        return handler.WriteToken(token);
    }
}
