using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.Shared.Authorization.Clients;
using PseudoMarkets.Shared.Authorization.Configuration;
using PseudoMarkets.Shared.Authorization.Contracts;

namespace PseudoMarkets.Shared.Authorization.Tests.Clients;

[TestFixture]
public class IdentityAuthorizationClientTests
{
    [Test]
    public async Task AuthorizeAsync_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        var sut = CreateSut(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)), new Uri("http://localhost:8080"));

        var result = await sut.AuthorizeAsync(string.Empty, "VIEW_MARKET_DATA");

        result.IsAuthorized.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.Unauthorized.GetHashCode());
        result.Detail.ShouldBe("A valid Bearer token is required.");
    }

    [Test]
    public async Task AuthorizeAsync_ShouldReturnDependencyFailure_WhenActionIsMissing()
    {
        var sut = CreateSut(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)), new Uri("http://localhost:8080"));

        var result = await sut.AuthorizeAsync("token", string.Empty);

        result.IsAuthorized.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable.GetHashCode());
        result.Detail.ShouldContain("Authorization action");
    }

    [Test]
    public async Task AuthorizeAsync_ShouldReturnDependencyFailure_WhenBaseAddressIsMissing()
    {
        var sut = CreateSut(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)), baseAddress: null);

        var result = await sut.AuthorizeAsync("token", "VIEW_MARKET_DATA");

        result.IsAuthorized.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable.GetHashCode());
        result.Detail.ShouldContain("not configured");
    }

    [Test]
    public async Task AuthorizeAsync_ShouldPostConfiguredRequestAndReturnAuthorized_WhenIdentityProviderApprovesRequest()
    {
        HttpRequestMessage? capturedRequest = null;
        var sut = CreateSut(
            request =>
            {
                capturedRequest = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = CreateJsonContent("""{"success":true,"message":"Authorization successful"}""")
                });
            },
            new Uri("http://localhost:8080"));

        var result = await sut.AuthorizeAsync("token", "VIEW_MARKET_DATA");

        result.IsAuthorized.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK.GetHashCode());
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Method.ShouldBe(HttpMethod.Post);
        capturedRequest.RequestUri.ShouldBe(new Uri("http://localhost:8080/api/identity/authorize"));

        var requestBody = await capturedRequest.Content!.ReadFromJsonAsync<IdentityAuthorizeRequest>();
        requestBody.ShouldNotBeNull();
        requestBody.Token.ShouldBe("token");
        requestBody.Action.ShouldBe("VIEW_MARKET_DATA");
    }

    [Test]
    public async Task AuthorizeAsync_ShouldReturnForbidden_WhenIdentityProviderRejectsRequest()
    {
        var sut = CreateSut(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = CreateJsonContent("""{"success":false,"message":"Unauthorized"}""")
            }),
            new Uri("http://localhost:8080"));

        var result = await sut.AuthorizeAsync("token", "VIEW_MARKET_DATA");

        result.IsAuthorized.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.Forbidden.GetHashCode());
        result.Detail.ShouldContain("Unauthorized");
    }

    [Test]
    public async Task AuthorizeAsync_ShouldReturnDependencyFailure_WhenIdentityProviderTimesOut()
    {
        var sut = CreateSut(_ => throw new TaskCanceledException("Timed out."), new Uri("http://localhost:8080"));

        var result = await sut.AuthorizeAsync("token", "VIEW_MARKET_DATA");

        result.IsAuthorized.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable.GetHashCode());
        result.Detail.ShouldContain("timed out");
    }

    [Test]
    public async Task AuthorizeAsync_ShouldReturnDependencyFailure_WhenIdentityProviderIsUnavailable()
    {
        var sut = CreateSut(_ => throw new HttpRequestException("Connection failed."), new Uri("http://localhost:8080"));

        var result = await sut.AuthorizeAsync("token", "VIEW_MARKET_DATA");

        result.IsAuthorized.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable.GetHashCode());
        result.Detail.ShouldContain("unavailable");
    }

    private static IdentityAuthorizationClient CreateSut(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory,
        Uri? baseAddress)
    {
        var configuration = Options.Create(new IdentityAuthorizationConfiguration
        {
            IdentityServerBaseUrl = baseAddress?.ToString() ?? string.Empty,
            AuthorizeEndpointPath = "/api/identity/authorize",
            TimeoutSeconds = 10
        });

        var httpClient = new HttpClient(new StubHttpMessageHandler(responseFactory));
        if (baseAddress is not null)
        {
            httpClient.BaseAddress = baseAddress;
        }

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
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _responseFactory(request);
        }
    }
}
