using Microsoft.Extensions.DependencyInjection;
using PseudoMarkets.MarketData.Core.Interfaces;
using PseudoMarkets.MarketData.Core.Services;

namespace PseudoMarkets.MarketData.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMarketDataCore(this IServiceCollection services)
    {
        services.AddScoped<IQuoteService, QuoteService>();

        return services;
    }
}
