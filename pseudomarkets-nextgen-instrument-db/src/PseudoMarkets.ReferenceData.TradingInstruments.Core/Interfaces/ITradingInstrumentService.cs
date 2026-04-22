using PseudoMarkets.ReferenceData.TradingInstruments.Contracts.Instruments;

namespace PseudoMarkets.ReferenceData.TradingInstruments.Core.Interfaces;

public interface ITradingInstrumentService
{
    Task<TradingInstrumentResponse> GetBySymbolAsync(string symbol, CancellationToken cancellationToken);

    Task<TradingInstrumentResponse> CreateAsync(
        CreateTradingInstrumentRequest request,
        CancellationToken cancellationToken);

    Task<TradingInstrumentResponse> UpdateClosingPriceAsync(
        string symbol,
        UpdateClosingPriceRequest request,
        CancellationToken cancellationToken);
}
