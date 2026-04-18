using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.MarketData.Contracts.Quotes;
using PseudoMarkets.MarketData.Core.Exceptions;
using PseudoMarkets.MarketData.Core.Interfaces;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;

namespace PseudoMarkets.MarketData.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketDataController : ControllerBase
{
    private readonly IQuoteService _quoteService;

    public MarketDataController(IQuoteService quoteService)
    {
        _quoteService = quoteService;
    }

    [AuthorizeWithIdentityServer(PlatformAuthorizationActions.ViewMarketData)]
    [HttpGet("quote/{symbol}")]
    public async Task<ActionResult<QuoteResponse>> GetQuote(string symbol, CancellationToken cancellationToken)
    {
        try
        {
            var quote = await _quoteService.GetLatestQuoteAsync(symbol, cancellationToken);
            if (quote is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Quote not found",
                    Detail = $"No quote was found for symbol '{symbol}'.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(quote);
        }
        catch (MarketDataValidationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid quote request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (MarketDataNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Quote not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (MarketDataDependencyException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Market data provider unavailable",
                Detail = ex.Message,
                Status = StatusCodes.Status503ServiceUnavailable
            });
        }
        catch (MarketDataServiceException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Market data service error",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    [AuthorizeWithIdentityServer(PlatformAuthorizationActions.ViewMarketData)]
    [HttpGet("quote/{symbol}/detailed")]
    public async Task<ActionResult<DetailedQuoteResponse>> GetDetailedQuote(string symbol, [FromQuery] string interval = "1min", CancellationToken cancellationToken = default)
    {
        try
        {
            var quote = await _quoteService.GetDetailedQuoteAsync(symbol, interval, cancellationToken);
            if (quote is null)
            {
                return NotFound(CreateProblem(StatusCodes.Status404NotFound, "Detailed quote not found", $"No detailed quote was found for symbol '{symbol}'."));
            }

            return Ok(quote);
        }
        catch (MarketDataValidationException ex)
        {
            return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "Invalid detailed quote request", ex.Message));
        }
        catch (MarketDataNotFoundException ex)
        {
            return NotFound(CreateProblem(StatusCodes.Status404NotFound, "Detailed quote not found", ex.Message));
        }
        catch (MarketDataDependencyException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, CreateProblem(StatusCodes.Status503ServiceUnavailable, "Market data provider unavailable", ex.Message));
        }
        catch (MarketDataServiceException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, CreateProblem(StatusCodes.Status500InternalServerError, "Market data service error", ex.Message));
        }
    }

    [AuthorizeWithIdentityServer(PlatformAuthorizationActions.ViewMarketData)]
    [HttpGet("indices")]
    public async Task<ActionResult<IndicesResponse>> GetIndices(CancellationToken cancellationToken)
    {
        try
        {
            var indices = await _quoteService.GetUsMarketIndicesAsync(cancellationToken);
            if (indices is null)
            {
                return NotFound(CreateProblem(StatusCodes.Status404NotFound, "Indices not found", "No U.S. market indices were found."));
            }

            return Ok(indices);
        }
        catch (MarketDataDependencyException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, CreateProblem(StatusCodes.Status503ServiceUnavailable, "Market data provider unavailable", ex.Message));
        }
        catch (MarketDataServiceException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, CreateProblem(StatusCodes.Status500InternalServerError, "Market data service error", ex.Message));
        }
    }

    private static ProblemDetails CreateProblem(int statusCode, string title, string detail)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode
        };
    }
}
