using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Exceptions;

namespace PseudoMarkets.ReferenceData.TradingInstruments.Service.Infrastructure;

public sealed class TradingInstrumentsExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public TradingInstrumentsExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            TradingInstrumentValidationException => (StatusCodes.Status400BadRequest, "Invalid trading instrument request"),
            TradingInstrumentNotFoundException => (StatusCodes.Status404NotFound, "Trading instrument not found"),
            TradingInstrumentConflictException => (StatusCodes.Status409Conflict, "Trading instrument conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Trading instrument service error")
        };

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message
            }
        });
    }
}
