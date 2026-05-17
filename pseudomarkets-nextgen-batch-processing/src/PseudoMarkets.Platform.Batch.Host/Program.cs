using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using PseudoMarkets.Platform.Batch.Core.Configuration;
using PseudoMarkets.Platform.Batch.Core.DependencyInjection;
using PseudoMarkets.Platform.Batch.Host.Infrastructure;
using PseudoMarkets.Shared.Entities.Database;
using PseudoMarkets.Shared.ServiceHelpers;

namespace PseudoMarkets.Platform.Batch.Host;

public class Program
{
    public static void Main(string[] args)
    {
        LoadSharedEnvironmentFile();

        var builder = WebApplication.CreateBuilder(args);
        var batchConfiguration = GetBatchConfiguration(builder.Configuration);

        builder.Services.AddAuthorization();
        builder.Services.AddProblemDetails();
        builder.Services.AddDbContext<PseudoMarketsDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("PseudoMarketsDb")));
        builder.Services.AddPseudoMarketsBatchCore(builder.Configuration);
        builder.Services.AddHealthChecks()
            .AddCheck<DbContextConnectivityHealthCheck<PseudoMarketsDbContext>>("postgres");
        builder.Services.AddHangfire((serviceProvider, configuration) =>
        {
            var storageConfiguration = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<BatchProcessingConfiguration>>()
                .Value
                .Storage;
            var connectionString = builder.Configuration.GetConnectionString("PseudoMarketsDb")
                ?? throw new InvalidOperationException("ConnectionStrings: PseudoMarketsDb must be configured.");

            configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
                {
                    SchemaName = storageConfiguration.SchemaName,
                    QueuePollInterval = TimeSpan.FromSeconds(storageConfiguration.QueuePollIntervalSeconds),
                    InvisibilityTimeout = TimeSpan.FromMinutes(storageConfiguration.InvisibilityTimeoutMinutes)
                });
        });

        if (batchConfiguration.Enabled)
        {
            builder.Services.AddHangfireServer(options =>
            {
                options.ServerName = batchConfiguration.Server.ServerName;
                options.WorkerCount = batchConfiguration.Server.WorkerCount;
                options.Queues = batchConfiguration.Server.Queues;
            });
        }

        var app = builder.Build();

        app.UseExceptionHandler();

        if (!string.Equals(
                Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            app.UseHttpsRedirection();
        }

        app.UseAuthorization();
        app.MapPseudoMarketsOperationalEndpoints<Program>();

        if (batchConfiguration.Dashboard.Enabled)
        {
            app.MapHangfireDashboard(
                batchConfiguration.Dashboard.Path,
                new DashboardOptions
                {
                    Authorization = [new AllowAllDashboardAuthorizationFilter()],
                    IsReadOnlyFunc = _ => batchConfiguration.Dashboard.ReadOnly
                })
                .AllowAnonymous();

            app.MapGet("/", () => Results.Redirect(batchConfiguration.Dashboard.Path));
        }
        else
        {
            app.MapGet("/", () => Results.Redirect("/info"));
        }

        app.Run();
    }

    private static BatchProcessingConfiguration GetBatchConfiguration(IConfiguration configuration)
    {
        return configuration.GetSection(BatchProcessingConfiguration.SectionName).Get<BatchProcessingConfiguration>()
            ?? new BatchProcessingConfiguration();
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
