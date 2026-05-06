using PseudoMarkets.ReferenceData.TradingInstruments.Contracts.Instruments;

namespace PseudoMarkets.OrderExecution.Core.Interfaces;

public interface ITradingInstrumentsClient
{
    Task<TradingInstrumentResponse> GetBySymbolAsync(string symbol, CancellationToken cancellationToken);
}
