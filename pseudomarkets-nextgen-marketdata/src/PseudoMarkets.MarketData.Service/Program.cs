using System;
using System.IO;
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
        LoadSharedEnvironmentFile();

        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile(
            $"appsettings.{builder.Environment.EnvironmentName}.json",
            optional: true,
            reloadOnChange: true);

        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
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
            app.UseSwagger();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1"));
        }

        app.UseExceptionHandler();

        if (!string.Equals(
                Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            app.UseHttpsRedirection();
        }

        app.MapHealthChecks("/health");
        app.MapControllers();

        app.Run();
    }

    private static void LoadSharedEnvironmentFile()
    {
        var envFilePath = FindEnvironmentFile(Directory.GetCurrentDirectory())
            ?? FindEnvironmentFile(AppContext.BaseDirectory);

        if (envFilePath is null)
        {
            return;
        }

        foreach (var rawLine in File.ReadLines(envFilePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                continue;
            }

            var value = line[(separatorIndex + 1)..].Trim().Trim('"');
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string? FindEnvironmentFile(string startPath)
    {
        DirectoryInfo? directory = Directory.Exists(startPath)
            ? new DirectoryInfo(startPath)
            : new FileInfo(startPath).Directory;

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
