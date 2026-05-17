using Microsoft.Extensions.Options;
using PseudoMarkets.Platform.Batch.Core.Configuration;
using PseudoMarkets.Platform.Batch.Core.Models;

namespace PseudoMarkets.Platform.Batch.Core.Services;

public sealed class BatchJobConfigurationResolver
{
    private readonly BatchProcessingConfiguration _configuration;

    public BatchJobConfigurationResolver(IOptions<BatchProcessingConfiguration> options)
    {
        _configuration = options.Value;
    }

    public ResolvedBatchJobConfiguration Resolve(BatchJobDefinition definition)
    {
        _configuration.Jobs.TryGetValue(definition.JobName, out var jobConfiguration);

        return new ResolvedBatchJobConfiguration(
            jobConfiguration?.Enabled ?? true,
            string.IsNullOrWhiteSpace(jobConfiguration?.CronExpression)
                ? definition.DefaultCronExpression
                : jobConfiguration.CronExpression,
            string.IsNullOrWhiteSpace(jobConfiguration?.Queue)
                ? definition.DefaultQueue
                : jobConfiguration.Queue,
            jobConfiguration?.DisableConcurrentExecution ?? definition.DisableConcurrentExecution,
            string.IsNullOrWhiteSpace(jobConfiguration?.TimeZoneId)
                ? definition.DefaultTimeZoneId
                : jobConfiguration.TimeZoneId);
    }
}
