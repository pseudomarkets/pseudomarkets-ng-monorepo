using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.OrderExecution.Core.Exceptions;
using PseudoMarkets.OrderExecution.Core.Models;

namespace PseudoMarkets.OrderExecution.Service.Infrastructure;

public sealed class OrderExecutionExceptionHandler : IExceptionHandler
{
    private readonly ILogger<OrderExecutionExceptionHandler> _logger;

    public OrderExecutionExceptionHandler(ILogger<OrderExecutionExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, code) = exception switch
        {
            OrderExecutionValidationException ex => (
                StatusCodes.Status400BadRequest,
                "Order validation failed",
                ex.Message,
                ex.Code),
            OrderExecutionAuthorizationException ex => (
                StatusCodes.Status403Forbidden,
                "Order authorization failed",
                ex.Message,
                ex.Code),
            OrderExecutionDependencyException ex when ex.Code == OrderExecutionErrorCodes.DownstreamUnauthorized => (
                StatusCodes.Status502BadGateway,
                "Downstream authorization failed",
                ex.Message,
                ex.Code),
            OrderExecutionDependencyException ex => (
                StatusCodes.Status503ServiceUnavailable,
                "Order dependency unavailable",
                ex.Message,
                ex.Code),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Order execution error",
                "The order execution service could not process the request.",
                "ORDER_EXECUTION_ERROR")
        };

        _logger.LogError(exception, "Order execution request failed with status code {StatusCode}.", statusCode);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };
        problemDetails.Extensions["code"] = code;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
