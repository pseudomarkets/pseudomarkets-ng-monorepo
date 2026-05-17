using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PseudoMarkets.Platform.Batch.Core.Configuration;
using PseudoMarkets.Platform.Batch.Core.Interfaces;

namespace PseudoMarkets.Platform.Batch.Core.Services;

public sealed class BatchJobRegistrationHostedService : IHostedService
{
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IBatchJobRegistry _jobRegistry;
    private readonly BatchJobConfigurationResolver _configurationResolver;
    private readonly IOptions<BatchProcessingConfiguration> _options;
    private readonly ILogger<BatchJobRegistrationHostedService> _logger;

    public BatchJobRegistrationHostedService(
        IRecurringJobManager recurringJobManager,
        IBatchJobRegistry jobRegistry,
        BatchJobConfigurationResolver configurationResolver,
        IOptions<BatchProcessingConfiguration> options,
        ILogger<BatchJobRegistrationHostedService> logger)
    {
        _recurringJobManager = recurringJobManager;
        _jobRegistry = jobRegistry;
        _configurationResolver = configurationResolver;
        _options = options;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Batch processing is disabled. No recurring jobs will be registered.");
            return Task.CompletedTask;
        }

        foreach (var definition in _jobRegistry.Definitions)
        {
            var configuration = _configurationResolver.Resolve(definition);

            if (!configuration.Enabled)
            {
                _recurringJobManager.RemoveIfExists(definition.JobName);
                _logger.LogInformation("Skipped disabled batch job {JobName}.", definition.JobName);
                continue;
            }

            if (string.IsNullOrWhiteSpace(configuration.CronExpression))
            {
                _logger.LogWarning(
                    "Skipped batch job {JobName} because no cron expression was provided.",
                    definition.JobName);
                continue;
            }

            var timeZone = ResolveTimeZone(configuration.TimeZoneId);
            var recurringJobOptions = new RecurringJobOptions
            {
#pragma warning disable CS0618
                QueueName = configuration.Queue,
#pragma warning restore CS0618
                TimeZone = timeZone
            };

            var job = Job.FromExpression<BatchJobInvoker>(
                invoker => invoker.ExecuteRecurringJobAsync(definition.JobName, CancellationToken.None));

            _recurringJobManager.AddOrUpdate(
                definition.JobName,
                job,
                configuration.CronExpression,
                recurringJobOptions);

            _logger.LogInformation(
                "Registered recurring batch job {JobName} with cron {CronExpression} on queue {Queue}.",
                definition.JobName,
                configuration.CronExpression,
                configuration.Queue);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }
}
