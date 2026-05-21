using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.MarketData.Contracts.Quotes;
using PseudoMarkets.MarketData.Core.Exceptions;
using PseudoMarkets.MarketData.Core.Interfaces;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;

namespace PseudoMarkets.MarketData.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MarketDataController : ControllerBase
{
    private readonly IQuoteService _quoteService;

    public MarketDataController(IQuoteService quoteService)
    {
        _quoteService = quoteService;
    }

    /// <summary>
    /// Gets the latest quote for a symbol.
    /// </summary>
    /// <remarks>
    /// Returns a lightweight latest-price quote for a tradable symbol. Responses may be served from Aerospike cache.
    /// Requires the VIEW_MARKET_DATA action.
    /// </remarks>
    [AuthorizeWithIdentityServer(PlatformAuthorizationActions.ViewMarketData)]
    [HttpGet("quote/{symbol}")]
    [EndpointSummary("Get latest quote")]
    [EndpointDescription("Returns the latest available quote for a symbol, including whether the response came from the provider or cache.")]
    [ProducesResponseType<QuoteResponse>(StatusCodes.Status200OK, Description = "The latest quote was found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, Description = "The symbol was invalid.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Description = "No quote was found for the symbol.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, Description = "The upstream market-data provider was unavailable.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, Description = "The market data service encountered an unexpected error.")]
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

    /// <summary>
    /// Gets a detailed quote for a symbol.
    /// </summary>
    /// <remarks>
    /// Returns open, high, low, close, previous close, and change fields supported by the configured data provider.
    /// Requires the VIEW_MARKET_DATA action.
    /// </remarks>
    [AuthorizeWithIdentityServer(PlatformAuthorizationActions.ViewMarketData)]
    [HttpGet("quote/{symbol}/detailed")]
    [EndpointSummary("Get detailed quote")]
    [EndpointDescription("Returns detailed quote fields for a symbol, including provider/cache source and quote timestamp.")]
    [ProducesResponseType<DetailedQuoteResponse>(StatusCodes.Status200OK, Description = "The detailed quote was found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, Description = "The symbol was invalid.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Description = "No detailed quote was found for the symbol.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, Description = "The upstream market-data provider was unavailable.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, Description = "The market data service encountered an unexpected error.")]
    public async Task<ActionResult<DetailedQuoteResponse>> GetDetailedQuote(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            var quote = await _quoteService.GetDetailedQuoteAsync(symbol, cancellationToken);
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

    /// <summary>
    /// Gets major U.S. market index snapshots.
    /// </summary>
    /// <remarks>
    /// Returns current values for the S&amp;P 500, Dow Jones Industrial Average, and NASDAQ indices.
    /// Requires the VIEW_MARKET_DATA action.
    /// </remarks>
    [AuthorizeWithIdentityServer(PlatformAuthorizationActions.ViewMarketData)]
    [HttpGet("indices")]
    [EndpointSummary("Get U.S. market indices")]
    [EndpointDescription("Returns current snapshots for the S&P 500, Dow Jones Industrial Average, and NASDAQ indices.")]
    [ProducesResponseType<IndicesResponse>(StatusCodes.Status200OK, Description = "The index snapshots were found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Description = "No U.S. market indices were found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, Description = "The upstream index-data provider was unavailable.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, Description = "The market data service encountered an unexpected error.")]
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
