using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PseudoMarkets.OrderExecution.Core.Configuration;
using PseudoMarkets.OrderExecution.Core.Exceptions;
using PseudoMarkets.OrderExecution.Core.Interfaces;
using PseudoMarkets.OrderExecution.Core.Models;

namespace PseudoMarkets.OrderExecution.Core.Services;

public sealed class SystemTokenProvider : ISystemTokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly OrderExecutionConfiguration _configuration;
    private readonly IClock _clock;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _cachedToken;
    private DateTime _expiresAtUtc;

    public SystemTokenProvider(
        HttpClient httpClient,
        IOptions<OrderExecutionConfiguration> configuration,
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

            if (string.IsNullOrWhiteSpace(_configuration.SystemAccountLoginId) ||
                string.IsNullOrWhiteSpace(_configuration.SystemAccountPassword))
            {
                throw new OrderExecutionDependencyException(
                    OrderExecutionErrorCodes.SystemTokenUnavailable,
                    "Order Execution system account credentials are not configured.");
            }

            using var response = await _httpClient.PostAsJsonAsync(
                "/api/identity/authenticate",
                new AuthenticateRequest(_configuration.SystemAccountLoginId, _configuration.SystemAccountPassword),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new OrderExecutionDependencyException(
                    OrderExecutionErrorCodes.SystemTokenUnavailable,
                    "Order Execution could not obtain a valid system account token.");
            }

            var payload = await response.Content.ReadFromJsonAsync<AuthenticateResponse>(cancellationToken);
            if (payload is null || !payload.Success || string.IsNullOrWhiteSpace(payload.Token))
            {
                throw new OrderExecutionDependencyException(
                    OrderExecutionErrorCodes.SystemTokenUnavailable,
                    "Order Execution received an invalid system account token response.");
            }

            _cachedToken = payload.Token;
            _expiresAtUtc = DateTime.SpecifyKind(payload.Expires, DateTimeKind.Utc);
            return _cachedToken;
        }
        catch (OrderExecutionException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new OrderExecutionDependencyException(
                OrderExecutionErrorCodes.SystemTokenUnavailable,
                "Order Execution could not reach the identity provider for system account authentication.",
                ex);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool HasValidCachedToken()
    {
        var refreshBuffer = TimeSpan.FromSeconds(_configuration.TokenRefreshBufferSeconds > 0
            ? _configuration.TokenRefreshBufferSeconds
            : 60);

        return !string.IsNullOrWhiteSpace(_cachedToken) &&
               _clock.UtcNow < _expiresAtUtc.Subtract(refreshBuffer);
    }

    private sealed record AuthenticateRequest(string LoginId, string Password);

    private sealed record AuthenticateResponse(bool Success, string Token, DateTime Expires);
}
