using Microsoft.Extensions.Options;
using PseudoMarkets.MarketData.Cache.DependencyInjection;
using PseudoMarkets.MarketData.Core.Configuration;
using PseudoMarkets.MarketData.Core.DependencyInjection;
using PseudoMarkets.MarketData.Providers.DependencyInjection;

namespace PseudoMarkets.MarketData.Service;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile(
            $"appsettings.{builder.Environment.EnvironmentName}.json",
            optional: true,
            reloadOnChange: true);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();
        builder.Services.Configure<AerospikeConfiguration>(builder.Configuration.GetRequiredSection("Aerospike"));
        builder.Services.Configure<TwelveDataConfiguration>(builder.Configuration.GetRequiredSection("TwelveData"));
        builder.Services.Configure<MarketDataCacheConfiguration>(builder.Configuration.GetRequiredSection("MarketDataCache"));
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AerospikeConfiguration>>().Value);
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<TwelveDataConfiguration>>().Value);
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<MarketDataCacheConfiguration>>().Value);
        builder.Services.AddMarketDataCore();
        builder.Services.AddMarketDataProviders();
        builder.Services.AddMarketDataCache();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseExceptionHandler();
        app.UseHttpsRedirection();
        app.MapHealthChecks("/health");
        app.MapControllers();

        app.Run();
    }
}
