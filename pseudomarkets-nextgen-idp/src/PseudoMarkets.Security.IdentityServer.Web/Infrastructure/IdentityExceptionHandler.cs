using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.Security.IdentityServer.Core.Exceptions;

namespace PseudoMarkets.Security.IdentityServer.Web.Infrastructure;

public class IdentityExceptionHandler : IExceptionHandler
{
    private readonly ILogger<IdentityExceptionHandler> _logger;

    public IdentityExceptionHandler(ILogger<IdentityExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            IdentityDependencyException => (
                StatusCodes.Status503ServiceUnavailable,
                "Identity data store unavailable",
                "The identity service could not reach its backing data store."),
            IdentityServiceException => (
                StatusCodes.Status500InternalServerError,
                "Identity service error",
                "The identity service could not process the request."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected server error",
                "An unexpected error occurred while processing the request.")
        };

        _logger.LogError(exception, "Request failed with status code {StatusCode}.", statusCode);

        httpContext.Response.StatusCode = statusCode;
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
