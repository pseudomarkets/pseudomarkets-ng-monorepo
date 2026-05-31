using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using PseudoMarkets.BalancesAndPositions.Core.Configuration;
using PseudoMarkets.BalancesAndPositions.Core.Services;
using PseudoMarkets.BalancesAndPositions.Tests.Support;
using PseudoMarkets.Shared.Authorization.Configuration;
using Shouldly;

namespace PseudoMarkets.BalancesAndPositions.Tests.Core;

[TestFixture]
public sealed class SystemTokenProviderTests
{
    [Test]
    public async Task GetTokenAsync_ShouldRefreshCachedSystemToken_WhenAccessTokenIsNearExpiry()
    {
        var clock = new FixedClock();
        var handler = new RecordingHandler(
        [
            HttpResponseMessageFactory.Success("token-1", clock.UtcNow.AddMinutes(60), "refresh-1", clock.UtcNow.AddMinutes(60)),
            HttpResponseMessageFactory.Success("token-2", clock.UtcNow.AddMinutes(119), "refresh-2", clock.UtcNow.AddMinutes(119))
        ]);

        var sut = CreateSut(handler, clock);

        var firstToken = await sut.GetTokenAsync(CancellationToken.None);
        clock.UtcNow = clock.UtcNow.AddMinutes(59).AddSeconds(1);
        var refreshedToken = await sut.GetTokenAsync(CancellationToken.None);

        firstToken.ShouldBe("token-1");
        refreshedToken.ShouldBe("token-2");
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldBe("/api/identity/authenticate");
        handler.Requests[1].RequestUri!.AbsolutePath.ShouldBe("/api/identity/refresh");
    }

    private static SystemTokenProvider CreateSut(RecordingHandler handler, FixedClock clock)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var factory = new StubHttpClientFactory(httpClient);
        var configuration = Options.Create(new BalancesAndPositionsConfiguration
        {
            SystemAccountLoginId = "system-user",
            SystemAccountPassword = "system-password",
            TokenRefreshBufferSeconds = 60
        });
        var identityAuthorization = Options.Create(new IdentityAuthorizationConfiguration
        {
            IdentityServerBaseUrl = "http://localhost:8080",
            TimeoutSeconds = 10
        });

        return new SystemTokenProvider(factory, configuration, clock);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public RecordingHandler(IEnumerable<HttpResponseMessage> responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No response configured.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public StubHttpClientFactory(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public HttpClient CreateClient(string name)
        {
            return _httpClient;
        }
    }

    private static class HttpResponseMessageFactory
    {
        public static HttpResponseMessage Success(string token, DateTime expires, string refreshToken, DateTime refreshTokenExpires)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    success = true,
                    token,
                    expires,
                    refreshToken,
                    refreshTokenExpires
                })
            };
        }
    }
}
