using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PseudoMarkets.OrderExecution.Core.Clients;
using PseudoMarkets.OrderExecution.Core.Configuration;
using PseudoMarkets.OrderExecution.Core.Interfaces;
using PseudoMarkets.OrderExecution.Core.Services;
using PseudoMarkets.Shared.Authorization.Configuration;

namespace PseudoMarkets.OrderExecution.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderExecutionCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrderExecutionConfiguration>(configuration.GetSection(OrderExecutionConfiguration.SectionName));
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IMarketHoursEvaluator, MarketHoursEvaluator>();
        services.AddScoped<IOrderSubmissionService, OrderSubmissionService>();

        services.AddHttpClient("OrderExecution.Identity", (serviceProvider, client) =>
        {
            var orderExecutionOptions = serviceProvider.GetRequiredService<IOptions<OrderExecutionConfiguration>>().Value;
            var identityAuthorizationOptions =
                serviceProvider.GetRequiredService<IOptions<IdentityAuthorizationConfiguration>>().Value;
            ConfigureClient(
                client,
                identityAuthorizationOptions.IdentityServerBaseUrl,
                orderExecutionOptions.TimeoutSeconds);
        });

        services.AddSingleton<ISystemTokenProvider>(serviceProvider =>
            new SystemTokenProvider(
                serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("OrderExecution.Identity"),
                serviceProvider.GetRequiredService<IOptions<OrderExecutionConfiguration>>(),
                serviceProvider.GetRequiredService<IClock>()));

        services.AddHttpClient<ITradingInstrumentsClient, TradingInstrumentsClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OrderExecutionConfiguration>>().Value;
            ConfigureClient(client, options.TradingInstrumentsBaseUrl, options.TimeoutSeconds);
        });

        services.AddHttpClient<IMarketDataClient, MarketDataClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OrderExecutionConfiguration>>().Value;
            ConfigureClient(client, options.MarketDataBaseUrl, options.TimeoutSeconds);
        });

        services.AddHttpClient<ITransactionProcessingClient, TransactionProcessingClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OrderExecutionConfiguration>>().Value;
            ConfigureClient(client, options.TransactionProcessingBaseUrl, options.TimeoutSeconds);
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
