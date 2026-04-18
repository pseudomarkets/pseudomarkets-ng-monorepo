using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PseudoMarkets.Shared.Authorization.Configuration;
using PseudoMarkets.Shared.Authorization.Contracts;
using PseudoMarkets.Shared.Authorization.Interfaces;
using PseudoMarkets.Shared.Authorization.Models;

namespace PseudoMarkets.Shared.Authorization.Clients;

public class IdentityAuthorizationClient : IIdentityAuthorizationClient
{
    private readonly HttpClient _httpClient;
    private readonly IdentityAuthorizationConfiguration _configuration;
    private readonly ILogger<IdentityAuthorizationClient> _logger;

    public IdentityAuthorizationClient(
        HttpClient httpClient,
        IOptions<IdentityAuthorizationConfiguration> configuration,
        ILogger<IdentityAuthorizationClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration.Value;
        _logger = logger;
    }

    public async Task<AuthorizationDecision> AuthorizeAsync(string token, string action, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthorizationDecision.Unauthorized("A valid Bearer token is required.");
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            return AuthorizationDecision.DependencyFailure("Authorization action was not configured for this endpoint.");
        }

        if (_httpClient.BaseAddress is null)
        {
            _logger.LogError("Identity authorization is unavailable because no identity server base URL was configured.");
            return AuthorizationDecision.DependencyFailure("Identity provider authorization is not configured.");
        }

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                _configuration.AuthorizeEndpointPath,
                new IdentityAuthorizeRequest(token, action),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var payload = await ReadAuthorizationResponseAsync(response, cancellationToken);
                return payload?.Success == true
                    ? AuthorizationDecision.Authorized()
                    : AuthorizationDecision.Forbidden(payload?.Message ?? $"The token is not authorized for action '{action}'.");
            }

            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
            {
                var payload = await ReadAuthorizationResponseAsync(response, cancellationToken);
                return AuthorizationDecision.Forbidden(payload?.Message ?? $"The token is not authorized for action '{action}'.");
            }

            _logger.LogWarning(
                "Identity provider authorization request failed with status code {StatusCode}.",
                (int)response.StatusCode);

            return AuthorizationDecision.DependencyFailure(
                $"Identity provider authorization request failed with status code {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Identity provider authorization request timed out.");
            return AuthorizationDecision.DependencyFailure("Identity provider authorization timed out.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Identity provider authorization request failed.");
            return AuthorizationDecision.DependencyFailure("Identity provider authorization is unavailable.");
        }
    }

    private static async Task<IdentityAuthorizationResponse?> ReadAuthorizationResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<IdentityAuthorizationResponse>(cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
