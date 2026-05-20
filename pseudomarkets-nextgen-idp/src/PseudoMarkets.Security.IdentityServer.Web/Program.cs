using System.Threading.RateLimiting;
using Aerospike.Client;
using Microsoft.Extensions.Options;
using PseudoMarkets.Security.IdentityServer.Core.Accounts.Implementations;
using PseudoMarkets.Security.IdentityServer.Core.Accounts.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Authentication.Implementations;
using PseudoMarkets.Security.IdentityServer.Core.Authentication.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Authorization.Implementations;
using PseudoMarkets.Security.IdentityServer.Core.Authorization.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Configuration;
using PseudoMarkets.Security.IdentityServer.Core.Database.Implementations;
using PseudoMarkets.Security.IdentityServer.Core.Database.Interfaces;
using PseudoMarkets.Security.IdentityServer.Web.Infrastructure;
using PseudoMarkets.Shared.ServiceHelpers;

namespace PseudoMarkets.Security.IdentityServer.Web;

public class Program
{
    public static void Main(string[] args)
    {
        LoadSharedEnvironmentFile();

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddAuthorization();
        builder.Services.AddExceptionHandler<IdentityExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddEndpointsApiExplorer();
        var healthChecks = builder.Services.AddHealthChecks();
        builder.Services.AddSwaggerGen();
        builder.Services.Configure<AerospikeConfiguration>(builder.Configuration.GetRequiredSection("Aerospike"));
        builder.Services.Configure<JwtConfiguration>(builder.Configuration.GetRequiredSection("JwtConfiguration"));
        builder.Services.Configure<IdentitySecurityConfiguration>(
            builder.Configuration.GetSection(IdentitySecurityConfiguration.SectionName));
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AerospikeConfiguration>>().Value);
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtConfiguration>>().Value);
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<IdentitySecurityConfiguration>>().Value);
        builder.Services.AddSingleton<IAerospikeClient>(sp =>
        {
            var configuration = sp.GetRequiredService<AerospikeConfiguration>();
            return new AerospikeClient(configuration.Host, configuration.Port);
        });
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("identity-sensitive", context =>
            {
                var securityConfiguration = context.RequestServices.GetRequiredService<IdentitySecurityConfiguration>();
                var permitLimit = securityConfiguration.SensitiveEndpointPermitLimit > 0
                    ? securityConfiguration.SensitiveEndpointPermitLimit
                    : 10;
                var window = TimeSpan.FromMinutes(
                    securityConfiguration.SensitiveEndpointWindowMinutes > 0
                        ? securityConfiguration.SensitiveEndpointWindowMinutes
                        : 1);
                var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = window,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });
        builder.Services.AddSingleton<IAccountRepository, AccountRepository>();
        builder.Services.AddSingleton<IAccountProvisioningManager, AccountProvisioningManager>();
        builder.Services.AddSingleton<IPasswordResetManager, PasswordResetManager>();
        builder.Services.AddSingleton<IAuthenticationManager, AuthenticationManager>();
        builder.Services.AddSingleton<IAuthorizationManager, AuthorizationManager>();
        healthChecks.AddCheck<AerospikeClientHealthCheck>("aerospike");

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

        app.UseRateLimiter();
        app.UseAuthorization();
        app.MapPseudoMarketsOperationalEndpoints<Program>();
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
