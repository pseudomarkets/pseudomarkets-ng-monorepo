using PseudoMarkets.ReferenceData.TradingInstruments.Core.Models;

namespace PseudoMarkets.ReferenceData.TradingInstruments.Core.Interfaces;

public interface ITradingInstrumentRepository
{
    Task<bool> ExistsAsync(string symbol, CancellationToken cancellationToken);

    Task<TradingInstrument?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken);

    Task<TradingInstrument> AddAsync(TradingInstrument instrument, CancellationToken cancellationToken);

    Task<TradingInstrument> UpdateClosingPriceAsync(
        string symbol,
        double closingPrice,
        DateOnly closingPriceDate,
        CancellationToken cancellationToken);
}
