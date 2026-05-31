using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.BalancesAndPositions.Core.Exceptions;

namespace PseudoMarkets.BalancesAndPositions.Service.Infrastructure;

public sealed class BalancesAndPositionsExceptionHandler : IExceptionHandler
{
    private readonly ILogger<BalancesAndPositionsExceptionHandler> _logger;

    public BalancesAndPositionsExceptionHandler(ILogger<BalancesAndPositionsExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            BalancesAndPositionsValidationException => (
                StatusCodes.Status400BadRequest,
                "Balances and positions validation failed",
                exception.Message),
            BalancesAndPositionsForbiddenException => (
                StatusCodes.Status403Forbidden,
                "Balances and positions access denied",
                exception.Message),
            BalancesAndPositionsNotFoundException => (
                StatusCodes.Status404NotFound,
                "Balances and positions record not found",
                exception.Message),
            BalancesAndPositionsDependencyException => (
                StatusCodes.Status503ServiceUnavailable,
                "Balances and positions dependency unavailable",
                exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Balances and positions service error",
                "An unexpected error occurred while processing the request.")
        };

        _logger.LogError(exception, "Balances and positions request failed with status code {StatusCode}.", statusCode);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            },
            cancellationToken);

        return true;
    }
}
