using Microsoft.Extensions.DependencyInjection;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Interfaces;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Services;

namespace PseudoMarkets.ReferenceData.TradingInstruments.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTradingInstrumentsCore(this IServiceCollection services)
    {
        services.AddScoped<ITradingInstrumentService, TradingInstrumentService>();

        return services;
    }
}
