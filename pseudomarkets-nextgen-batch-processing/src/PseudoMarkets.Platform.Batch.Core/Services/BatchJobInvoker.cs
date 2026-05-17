using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PseudoMarkets.Platform.Batch.Core.Interfaces;

namespace PseudoMarkets.Platform.Batch.Core.Services;

public sealed class BatchJobInvoker
{
    private static readonly TimeSpan DistributedLockTimeout = TimeSpan.FromMinutes(5);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBatchJobRegistry _jobRegistry;
    private readonly IBatchJobLockProvider _lockProvider;
    private readonly BatchJobConfigurationResolver _configurationResolver;
    private readonly ILogger<BatchJobInvoker> _logger;

    public BatchJobInvoker(
        IServiceScopeFactory scopeFactory,
        IBatchJobRegistry jobRegistry,
        IBatchJobLockProvider lockProvider,
        BatchJobConfigurationResolver configurationResolver,
        ILogger<BatchJobInvoker> logger)
    {
        _scopeFactory = scopeFactory;
        _jobRegistry = jobRegistry;
        _lockProvider = lockProvider;
        _configurationResolver = configurationResolver;
        _logger = logger;
    }

    public async Task ExecuteRecurringJobAsync(string jobName, CancellationToken cancellationToken)
    {
        var definition = _jobRegistry.GetRequiredDefinition(jobName);
        var configuration = _configurationResolver.Resolve(definition);

        using var distributedLock = configuration.DisableConcurrentExecution
            ? _lockProvider.Acquire(jobName, DistributedLockTimeout)
            : null;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var job = (IBatchJob)scope.ServiceProvider.GetRequiredService(definition.JobType);

        _logger.LogInformation("Executing batch job {JobName}.", jobName);
        await job.ExecuteAsync(cancellationToken);
        _logger.LogInformation("Completed batch job {JobName}.", jobName);
    }
}
