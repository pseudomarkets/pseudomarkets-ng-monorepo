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
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Tests.Authorization.Implementations;

[TestFixture]
public class AuthorizationManagerTests
{
    private JwtConfiguration _jwtConfiguration = null!;
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

        _sut = new AuthorizationManager(_jwtConfiguration, Mock.Of<ILogger<AuthorizationManager>>());
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
        var token = CreateToken("roles", "VIEW_BALANCES,UPDATE_BALANCES");

        var result = _sut.Authorize(new AuthorizationRequest(token, "VIEW_BALANCES"));

        result.Success.ShouldBeTrue();
        result.Message.ShouldBe("Authorization Successful");
    }

    [Test]
    public void Authorize_ShouldReturnUnauthorized_WhenRolesClaimDoesNotContainAction()
    {
        var token = CreateToken("roles", "UPDATE_BALANCES");

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
        var token = CreateToken(ClaimTypes.Role, "VIEW_BALANCES");

        var result = _sut.Authorize(new AuthorizationRequest(token, "VIEW_BALANCES"));

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Authorization Failed");
    }

    private string CreateToken(string claimType, string claimValue)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "user"),
            new Claim("id", "1000000000"),
            new Claim(claimType, claimValue)
        };

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
