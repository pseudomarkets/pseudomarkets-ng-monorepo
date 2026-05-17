using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PseudoMarkets.Platform.Batch.Core.Configuration;
using PseudoMarkets.Platform.Batch.Core.Interfaces;
using PseudoMarkets.Platform.Batch.Core.Models;
using PseudoMarkets.Platform.Batch.Core.Services;

namespace PseudoMarkets.Platform.Batch.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPseudoMarketsBatchCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BatchProcessingConfiguration>(
            configuration.GetSection(BatchProcessingConfiguration.SectionName));

        services.TryAddSingleton<IBatchJobRegistry, BatchJobRegistry>();
        services.TryAddSingleton<IBatchJobLockProvider, HangfireDistributedLockProvider>();
        services.TryAddSingleton<BatchJobConfigurationResolver>();
        services.TryAddScoped<BatchJobInvoker>();
        services.AddHostedService<BatchJobRegistrationHostedService>();

        return services;
    }

    public static IServiceCollection AddBatchJob<TJob>(
        this IServiceCollection services,
        string jobName,
        string defaultCronExpression,
        Action<BatchJobRegistrationOptions>? configure = null)
        where TJob : class, IBatchJob
    {
        var options = new BatchJobRegistrationOptions();
        configure?.Invoke(options);

        services.AddScoped<TJob>();
        services.AddSingleton(new BatchJobDefinition(
            typeof(TJob),
            jobName,
            defaultCronExpression,
            options.Queue,
            options.DisableConcurrentExecution,
            options.TimeZoneId));

        return services;
    }
}
