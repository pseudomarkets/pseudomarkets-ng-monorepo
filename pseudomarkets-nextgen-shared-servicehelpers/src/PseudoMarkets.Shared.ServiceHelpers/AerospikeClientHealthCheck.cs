using Aerospike.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PseudoMarkets.Shared.ServiceHelpers;

public sealed class AerospikeClientHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    public AerospikeClientHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _serviceProvider.GetRequiredService<IAerospikeClient>();
            return Task.FromResult(
                client.Connected
                    ? HealthCheckResult.Healthy("Aerospike client is connected.")
                    : HealthCheckResult.Unhealthy("Aerospike client is not connected."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Aerospike connectivity check failed.", ex));
        }
    }
}
