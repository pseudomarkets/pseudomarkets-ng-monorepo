using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.ReferenceData.TradingInstruments.Contracts.Instruments;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Interfaces;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;

namespace PseudoMarkets.ReferenceData.TradingInstruments.Service.Controllers;

[ApiController]
[Route("api/trading-instruments")]
[Produces("application/json")]
public sealed class TradingInstrumentsController : ControllerBase
{
    private readonly ITradingInstrumentService _tradingInstrumentService;

    public TradingInstrumentsController(ITradingInstrumentService tradingInstrumentService)
    {
        _tradingInstrumentService = tradingInstrumentService;
    }

    /// <summary>
    /// Gets a trading instrument by symbol.
    /// </summary>
    /// <remarks>
    /// Returns the reference-data record for a platform-supported trading instrument.
    /// Requires the VIEW_MARKET_DATA action.
    /// </remarks>
    [HttpGet("{symbol}")]
    [AuthorizeWithIdentityServer(PlatformAuthorizationActions.ViewMarketData)]
    [EndpointSummary("Get trading instrument")]
    [EndpointDescription("Returns a supported trading instrument by symbol.")]
    [ProducesResponseType<TradingInstrumentResponse>(StatusCodes.Status200OK, Description = "The trading instrument was found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, Description = "The token is not authorized to view market data.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Description = "The trading instrument was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, Description = "The trading instruments service encountered an unexpected error.")]
    public async Task<ActionResult<TradingInstrumentResponse>> GetBySymbol(
        string symbol,
        CancellationToken cancellationToken)
    {
        return Ok(await _tradingInstrumentService.GetBySymbolAsync(symbol, cancellationToken));
    }

    /// <summary>
    /// Creates a trading instrument.
    /// </summary>
    /// <remarks>
    /// Adds a new trading instrument to the platform reference database. New instruments are initially tradable.
    /// Requires the UPDATE_INSTRUMENTS action.
    /// </remarks>
    [HttpPost]
    [AuthorizeWithIdentityServer(PlatformAuthorizationActions.UpdateInstruments)]
    [EndpointSummary("Create trading instrument")]
    [EndpointDescription("Creates a new platform-supported trading instrument.")]
    [ProducesResponseType<TradingInstrumentResponse>(StatusCodes.Status201Created, Description = "The trading instrument was created.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, Description = "The create request failed validation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, Description = "The token is not authorized to update instruments.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, Description = "A trading instrument with the same symbol already exists.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, Description = "The trading instruments service encountered an unexpected error.")]
    public async Task<ActionResult<TradingInstrumentResponse>> Create(
        [FromBody] CreateTradingInstrumentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _tradingInstrumentService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetBySymbol), new { symbol = response.Symbol }, response);
    }

    /// <summary>
    /// Updates a trading instrument closing price.
    /// </summary>
    /// <remarks>
    /// Updates the most recent closing price and optional close date for a supported trading instrument.
    /// Requires the UPDATE_INSTRUMENTS action.
    /// </remarks>
    [HttpPatch("{symbol}/closing-price")]
    [AuthorizeWithIdentityServer(PlatformAuthorizationActions.UpdateInstruments)]
    [EndpointSummary("Update closing price")]
    [EndpointDescription("Updates the latest closing price for a trading instrument.")]
    [ProducesResponseType<TradingInstrumentResponse>(StatusCodes.Status200OK, Description = "The closing price was updated.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, Description = "The closing-price request failed validation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, Description = "The token is not authorized to update instruments.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Description = "The trading instrument was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, Description = "The trading instruments service encountered an unexpected error.")]
    public async Task<ActionResult<TradingInstrumentResponse>> UpdateClosingPrice(
        string symbol,
        [FromBody] UpdateClosingPriceRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _tradingInstrumentService.UpdateClosingPriceAsync(symbol, request, cancellationToken));
    }
}
