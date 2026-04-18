using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.Shared.Authorization.Clients;
using PseudoMarkets.Shared.Authorization.Configuration;

namespace PseudoMarkets.MarketData.Tests.Authorization;

[TestFixture]
public class IdentityAuthorizationClientTests
{
    [Test]
    public async Task AuthorizeAsync_ShouldReturnAuthorized_WhenIdentityProviderApprovesRequest()
    {
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = CreateJsonContent("""{"success":true,"message":"Authorization Successful"}""")
        });

        var result = await sut.AuthorizeAsync("token", "VIEW_MARKET_DATA");

        result.IsAuthorized.ShouldBeTrue();
        result.StatusCode.ShouldBe(200);
    }

    [Test]
    public async Task AuthorizeAsync_ShouldReturnForbidden_WhenIdentityProviderRejectsRequest()
    {
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = CreateJsonContent("""{"success":false,"message":"Unauthorized"}""")
        });

        var result = await sut.AuthorizeAsync("token", "VIEW_MARKET_DATA");

        result.IsAuthorized.ShouldBeFalse();
        result.StatusCode.ShouldBe(403);
        result.Detail.ShouldContain("Unauthorized");
    }

    [Test]
    public async Task AuthorizeAsync_ShouldReturnDependencyFailure_WhenIdentityProviderIsUnavailable()
    {
        var sut = CreateSut(_ => throw new HttpRequestException("Connection failed."));

        var result = await sut.AuthorizeAsync("token", "VIEW_MARKET_DATA");

        result.IsAuthorized.ShouldBeFalse();
        result.StatusCode.ShouldBe(503);
    }

    private static IdentityAuthorizationClient CreateSut(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var configuration = Options.Create(new IdentityAuthorizationConfiguration
        {
            IdentityServerBaseUrl = "http://localhost:8080",
            AuthorizeEndpointPath = "/api/identity/authorize",
            TimeoutSeconds = 10
        });

        var httpClient = new HttpClient(new StubHttpMessageHandler(responseFactory))
        {
            BaseAddress = new Uri(configuration.Value.IdentityServerBaseUrl)
        };

        return new IdentityAuthorizationClient(
            httpClient,
            configuration,
            Mock.Of<ILogger<IdentityAuthorizationClient>>());
    }

    private static StringContent CreateJsonContent(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
