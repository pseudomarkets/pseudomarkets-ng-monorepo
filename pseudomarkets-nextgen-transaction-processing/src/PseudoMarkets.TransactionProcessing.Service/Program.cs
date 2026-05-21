using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using PseudoMarkets.Shared.Authorization.DependencyInjection;
using PseudoMarkets.Shared.Entities.Database;
using PseudoMarkets.Shared.ServiceHelpers;
using PseudoMarkets.TransactionProcessing.Core.DependencyInjection;
using PseudoMarkets.TransactionProcessing.Persistence.DependencyInjection;
using PseudoMarkets.TransactionProcessing.Service.Infrastructure;
using Scalar.AspNetCore;

namespace PseudoMarkets.TransactionProcessing.Service;

public class Program
{
    public static void Main(string[] args)
    {
        LoadSharedEnvironmentFile();

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddAuthorization();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<TransactionProcessingExceptionHandler>();
        var healthChecks = builder.Services.AddHealthChecks();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Enter a valid IDP JWT Bearer token."
                    }
                };

                foreach (var path in document.Paths.Values)
                {
                    if (path.Operations is null)
                    {
                        continue;
                    }

                    foreach (var operation in path.Operations.Values)
                    {
                        operation.Security ??= [];
                        operation.Security.Add(new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                        });
                    }
                }

                return Task.CompletedTask;
            });
        });
        builder.Services.AddTransactionProcessingCore();
        builder.Services.AddTransactionProcessingPersistence(builder.Configuration);
        builder.Services.AddPseudoMarketsSharedAuthorization(builder.Configuration);
        healthChecks.AddCheck<DbContextConnectivityHealthCheck<PseudoMarketsDbContext>>("postgres");

        var app = builder.Build();
        ApplyEfCoreMigration(app);

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.AddPreferredSecuritySchemes("Bearer");
            });
        }

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
        app.MapControllers();

        app.Run();
    }

    private static void ApplyEfCoreMigration(WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PseudoMarketsDbContext>();
            db.Database.Migrate();
        }
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
