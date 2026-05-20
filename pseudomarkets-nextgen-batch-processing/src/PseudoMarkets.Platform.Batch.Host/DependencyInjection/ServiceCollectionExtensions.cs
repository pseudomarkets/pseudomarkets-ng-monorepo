using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PseudoMarkets.Platform.Batch.Core.DependencyInjection;
using PseudoMarkets.Platform.Batch.Host.Clients;
using PseudoMarkets.Platform.Batch.Host.Configuration;
using PseudoMarkets.Platform.Batch.Host.Interfaces;
using PseudoMarkets.Platform.Batch.Host.Jobs;
using PseudoMarkets.Platform.Batch.Host.Repositories;
using PseudoMarkets.Platform.Batch.Host.Services;

namespace PseudoMarkets.Platform.Batch.Host.DependencyInjection;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQueuedOrderExecutionBatchProcessing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<QueuedOrderExecutionConfiguration>(
            configuration.GetSection(QueuedOrderExecutionConfiguration.SectionName));

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IQueuedOrderRepository, QueuedOrderRepository>();
        services.AddScoped<IMarketOpenEvaluator, MarketOpenEvaluator>();
        services.AddScoped<QueuedOrderExecutionJob>();

        services.AddHttpClient("QueuedOrderExecution.Identity", (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<QueuedOrderExecutionConfiguration>>().Value;
            ConfigureClient(client, options.IdentityServerBaseUrl, options.TimeoutSeconds);
        });

        services.AddSingleton<IQueuedOrderExecutionTokenProvider>(serviceProvider =>
            new QueuedOrderExecutionTokenProvider(
                serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("QueuedOrderExecution.Identity"),
                serviceProvider.GetRequiredService<IOptions<QueuedOrderExecutionConfiguration>>(),
                serviceProvider.GetRequiredService<IClock>()));

        services.AddHttpClient<IOrderExecutionClient, OrderExecutionClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<QueuedOrderExecutionConfiguration>>().Value;
            ConfigureClient(client, options.OrderExecutionBaseUrl, options.TimeoutSeconds);
        });

        services.AddBatchJob<QueuedOrderExecutionJob>(
            QueuedOrderExecutionJob.JobName,
            QueuedOrderExecutionJob.DefaultCronExpression,
            options =>
            {
                options.Queue = "default";
                options.DisableConcurrentExecution = true;
                options.TimeZoneId = QueuedOrderExecutionJob.TimeZoneId;
            });

        return services;
    }

    private static void ConfigureClient(HttpClient client, string baseUrl, int timeoutSeconds)
    {
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseAddress))
        {
            client.BaseAddress = baseAddress;
        }

        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds > 0 ? timeoutSeconds : 10);
    }
}
