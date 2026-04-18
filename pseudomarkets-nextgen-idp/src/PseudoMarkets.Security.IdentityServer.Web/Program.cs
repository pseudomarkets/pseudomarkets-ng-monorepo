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

namespace PseudoMarkets.Security.IdentityServer.Web;

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
        builder.Services.AddAuthorization();
        builder.Services.AddExceptionHandler<IdentityExceptionHandler>();
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.Configure<AerospikeConfiguration>(builder.Configuration.GetRequiredSection("Aerospike"));
        builder.Services.Configure<JwtConfiguration>(builder.Configuration.GetRequiredSection("JwtConfiguration"));
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AerospikeConfiguration>>().Value);
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtConfiguration>>().Value);
        builder.Services.AddSingleton<IAccountRepository, AccountRepository>();
        builder.Services.AddSingleton<IAccountProvisioningManager, AccountProvisioningManager>();
        builder.Services.AddSingleton<IAuthenticationManager, AuthenticationManager>();
        builder.Services.AddSingleton<IAuthorizationManager, AuthorizationManager>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "v1");
            });
        }

        app.UseExceptionHandler();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
