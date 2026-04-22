using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.ReferenceData.TradingInstruments.Contracts.Instruments;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Interfaces;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;

namespace PseudoMarkets.ReferenceData.TradingInstruments.Service.Controllers;

[ApiController]
[Route("api/trading-instruments")]
public sealed class TradingInstrumentsController : ControllerBase
{
    private readonly ITradingInstrumentService _tradingInstrumentService;

    public TradingInstrumentsController(ITradingInstrumentService tradingInstrumentService)
    {
        _tradingInstrumentService = tradingInstrumentService;
    }

    [HttpGet("{symbol}")]
    [AuthorizeWithIdentityServer(PlatformAuthorizationActions.ViewMarketData)]
    public async Task<ActionResult<TradingInstrumentResponse>> GetBySymbol(
        string symbol,
        CancellationToken cancellationToken)
    {
        return Ok(await _tradingInstrumentService.GetBySymbolAsync(symbol, cancellationToken));
    }

    [HttpPost]
    [AuthorizeWithIdentityServer(PlatformAuthorizationActions.UpdateInstruments)]
    public async Task<ActionResult<TradingInstrumentResponse>> Create(
        [FromBody] CreateTradingInstrumentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _tradingInstrumentService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetBySymbol), new { symbol = response.Symbol }, response);
    }

    [HttpPatch("{symbol}/closing-price")]
    [AuthorizeWithIdentityServer(PlatformAuthorizationActions.UpdateInstruments)]
    public async Task<ActionResult<TradingInstrumentResponse>> UpdateClosingPrice(
        string symbol,
        [FromBody] UpdateClosingPriceRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _tradingInstrumentService.UpdateClosingPriceAsync(symbol, request, cancellationToken));
    }
}
