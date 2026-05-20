using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using PseudoMarkets.OrderExecution.Core.Configuration;
using PseudoMarkets.OrderExecution.Core.Services;
using PseudoMarkets.OrderExecution.Tests.Support;
using Shouldly;

namespace PseudoMarkets.OrderExecution.Tests.Core;

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
        handler.RequestBodies[1].ShouldContain("\"refreshToken\":\"refresh-1\"");
    }

    [Test]
    public async Task GetTokenAsync_ShouldReauthenticate_WhenRefreshRequestFails()
    {
        var clock = new FixedClock();
        var handler = new RecordingHandler(
        [
            HttpResponseMessageFactory.Success("token-1", clock.UtcNow.AddMinutes(60), "refresh-1", clock.UtcNow.AddMinutes(60)),
            new HttpResponseMessage(HttpStatusCode.Unauthorized),
            HttpResponseMessageFactory.Success("token-3", clock.UtcNow.AddMinutes(119), "refresh-3", clock.UtcNow.AddMinutes(119))
        ]);

        var sut = CreateSut(handler, clock);

        var firstToken = await sut.GetTokenAsync(CancellationToken.None);
        clock.UtcNow = clock.UtcNow.AddMinutes(59).AddSeconds(1);
        var refreshedToken = await sut.GetTokenAsync(CancellationToken.None);

        firstToken.ShouldBe("token-1");
        refreshedToken.ShouldBe("token-3");
        handler.Requests.Count.ShouldBe(3);
        handler.Requests[1].RequestUri!.AbsolutePath.ShouldBe("/api/identity/refresh");
        handler.Requests[2].RequestUri!.AbsolutePath.ShouldBe("/api/identity/authenticate");
    }

    private static SystemTokenProvider CreateSut(RecordingHandler handler, FixedClock clock)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var options = Options.Create(new OrderExecutionConfiguration
        {
            SystemAccountLoginId = "system-user",
            SystemAccountPassword = "system-password",
            TokenRefreshBufferSeconds = 60
        });

        return new SystemTokenProvider(httpClient, options, clock);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public RecordingHandler(IEnumerable<HttpResponseMessage> responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No response configured.");
            }

            return _responses.Dequeue();
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
