using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.TransactionProcessing.Core.Exceptions;

namespace PseudoMarkets.TransactionProcessing.Service.Infrastructure;

public class TransactionProcessingExceptionHandler : IExceptionHandler
{
    private readonly ILogger<TransactionProcessingExceptionHandler> _logger;

    public TransactionProcessingExceptionHandler(ILogger<TransactionProcessingExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            TransactionProcessingValidationException => (
                StatusCodes.Status400BadRequest,
                "Transaction validation failed",
                exception.Message),
            TransactionProcessingNotFoundException => (
                StatusCodes.Status404NotFound,
                "Transaction not found",
                exception.Message),
            TransactionProcessingConflictException => (
                StatusCodes.Status409Conflict,
                "Transaction conflict",
                exception.Message),
            TransactionProcessingDependencyException => (
                StatusCodes.Status503ServiceUnavailable,
                "Transaction persistence unavailable",
                "The transaction processing service could not reach its database."),
            TransactionProcessingServiceException => (
                StatusCodes.Status500InternalServerError,
                "Transaction processing error",
                "The transaction processing service could not process the request."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected server error",
                "An unexpected error occurred while processing the request.")
        };

        _logger.LogError(exception, "Transaction processing request failed with status code {StatusCode}.", statusCode);

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
