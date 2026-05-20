using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PseudoMarkets.Platform.Batch.Host.Configuration;
using PseudoMarkets.Platform.Batch.Host.Interfaces;

namespace PseudoMarkets.Platform.Batch.Host.Services;

internal sealed class QueuedOrderExecutionTokenProvider : IQueuedOrderExecutionTokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly QueuedOrderExecutionConfiguration _configuration;
    private readonly IClock _clock;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _cachedToken;
    private DateTime _accessTokenExpiresAtUtc;
    private string? _cachedRefreshToken;
    private DateTime _refreshTokenExpiresAtUtc;

    public QueuedOrderExecutionTokenProvider(
        HttpClient httpClient,
        IOptions<QueuedOrderExecutionConfiguration> configuration,
        IClock clock)
    {
        _httpClient = httpClient;
        _configuration = configuration.Value;
        _clock = clock;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (HasValidCachedToken())
        {
            return _cachedToken!;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (HasValidCachedToken())
            {
                return _cachedToken!;
            }

            if (HasUsableRefreshToken() && await TryRefreshAsync(cancellationToken))
            {
                return _cachedToken!;
            }

            return await AuthenticateAsync(cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<string> AuthenticateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_configuration.SystemAccountLoginId) ||
            string.IsNullOrWhiteSpace(_configuration.SystemAccountPassword))
        {
            throw new InvalidOperationException(
                "Queued order execution system account credentials are not configured.");
        }

        using var response = await _httpClient.PostAsJsonAsync(
            "/api/identity/authenticate",
            new AuthenticateRequest(_configuration.SystemAccountLoginId, _configuration.SystemAccountPassword),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Unable to obtain a queued order execution system token.");
        }

        var payload = await response.Content.ReadFromJsonAsync<AuthenticateResponse>(cancellationToken);
        if (payload is null || !payload.Success || string.IsNullOrWhiteSpace(payload.Token))
        {
            throw new InvalidOperationException("Received an invalid queued order execution token response.");
        }

        CacheTokens(payload);
        return _cachedToken!;
    }

    private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "/api/identity/refresh",
                new RefreshTokenRequest(_cachedRefreshToken!),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                ClearCachedRefreshToken();
                return false;
            }

            var payload = await response.Content.ReadFromJsonAsync<AuthenticateResponse>(cancellationToken);
            if (payload is null || !payload.Success || string.IsNullOrWhiteSpace(payload.Token) ||
                string.IsNullOrWhiteSpace(payload.RefreshToken))
            {
                ClearCachedRefreshToken();
                return false;
            }

            CacheTokens(payload);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private bool HasValidCachedToken()
    {
        var refreshBuffer = TimeSpan.FromSeconds(_configuration.TokenRefreshBufferSeconds > 0
            ? _configuration.TokenRefreshBufferSeconds
            : 60);

        return !string.IsNullOrWhiteSpace(_cachedToken) &&
               _clock.UtcNow < _accessTokenExpiresAtUtc.Subtract(refreshBuffer);
    }

    private bool HasUsableRefreshToken()
    {
        return !string.IsNullOrWhiteSpace(_cachedRefreshToken) &&
               _clock.UtcNow < _refreshTokenExpiresAtUtc;
    }

    private void CacheTokens(AuthenticateResponse payload)
    {
        _cachedToken = payload.Token;
        _accessTokenExpiresAtUtc = DateTime.SpecifyKind(payload.Expires, DateTimeKind.Utc);
        _cachedRefreshToken = payload.RefreshToken;
        _refreshTokenExpiresAtUtc = DateTime.SpecifyKind(payload.RefreshTokenExpires, DateTimeKind.Utc);
    }

    private void ClearCachedRefreshToken()
    {
        _cachedRefreshToken = null;
        _refreshTokenExpiresAtUtc = DateTime.MinValue;
    }

    private sealed record AuthenticateRequest(string LoginId, string Password);
    private sealed record RefreshTokenRequest(string RefreshToken);

    private sealed record AuthenticateResponse(
        bool Success,
        string Token,
        DateTime Expires,
        string RefreshToken,
        DateTime RefreshTokenExpires);
}
