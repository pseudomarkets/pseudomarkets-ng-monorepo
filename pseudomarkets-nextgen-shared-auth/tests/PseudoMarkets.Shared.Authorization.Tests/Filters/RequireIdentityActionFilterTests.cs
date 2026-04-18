using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.Shared.Authorization.Filters;
using PseudoMarkets.Shared.Authorization.Interfaces;
using PseudoMarkets.Shared.Authorization.Models;

namespace PseudoMarkets.Shared.Authorization.Tests.Filters;

[TestFixture]
public class RequireIdentityActionFilterTests
{
    private Mock<IIdentityAuthorizationClient> _identityAuthorizationClient = null!;
    private RequireIdentityActionFilter _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _identityAuthorizationClient = new Mock<IIdentityAuthorizationClient>();
        _sut = new RequireIdentityActionFilter(
            "VIEW_MARKET_DATA",
            _identityAuthorizationClient.Object,
            Mock.Of<ILogger<RequireIdentityActionFilter>>());
    }

    [Test]
    public async Task OnAuthorizationAsync_ShouldSkipAuthorization_WhenAllowAnonymousIsPresent()
    {
        var context = CreateContext(endpointMetadata: new AllowAnonymousAttribute());

        await _sut.OnAuthorizationAsync(context);

        context.Result.ShouldBeNull();
        _identityAuthorizationClient.Verify(
            x => x.AuthorizeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task OnAuthorizationAsync_ShouldReturnUnauthorized_WhenAuthorizationHeaderIsMissing()
    {
        var context = CreateContext();

        await _sut.OnAuthorizationAsync(context);

        var result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Test]
    public async Task OnAuthorizationAsync_ShouldReturnUnauthorized_WhenAuthorizationSchemeIsInvalid()
    {
        var context = CreateContext("Basic token");

        await _sut.OnAuthorizationAsync(context);

        var result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Test]
    public async Task OnAuthorizationAsync_ShouldPassBearerTokenToIdentityProvider_WhenTokenIsPresent()
    {
        _identityAuthorizationClient
            .Setup(x => x.AuthorizeAsync("token", "VIEW_MARKET_DATA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthorizationDecision.Authorized());

        var context = CreateContext("Bearer token");

        await _sut.OnAuthorizationAsync(context);

        context.Result.ShouldBeNull();
        _identityAuthorizationClient.Verify(
            x => x.AuthorizeAsync("token", "VIEW_MARKET_DATA", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task OnAuthorizationAsync_ShouldReturnForbidden_WhenIdentityProviderRejectsToken()
    {
        _identityAuthorizationClient
            .Setup(x => x.AuthorizeAsync("token", "VIEW_MARKET_DATA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthorizationDecision.Forbidden("Unauthorized"));

        var context = CreateContext("Bearer token");

        await _sut.OnAuthorizationAsync(context);

        var result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Test]
    public async Task OnAuthorizationAsync_ShouldReturnDependencyFailure_WhenIdentityProviderIsUnavailable()
    {
        _identityAuthorizationClient
            .Setup(x => x.AuthorizeAsync("token", "VIEW_MARKET_DATA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthorizationDecision.DependencyFailure("Identity provider authorization is unavailable."));

        var context = CreateContext("Bearer token");

        await _sut.OnAuthorizationAsync(context);

        var result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
    }

    private static AuthorizationFilterContext CreateContext(string? authorizationHeader = null, object? endpointMetadata = null)
    {
        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            httpContext.Request.Headers.Authorization = authorizationHeader;
        }

        if (endpointMetadata is not null)
        {
            httpContext.SetEndpoint(new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(endpointMetadata),
                "test"));
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, []);
    }
}
