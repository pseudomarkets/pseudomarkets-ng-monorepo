using Microsoft.EntityFrameworkCore;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Exceptions;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Interfaces;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Models;
using PseudoMarkets.Shared.Entities.Database;
using PseudoMarkets.Shared.Entities.Entities.ReferenceData;

namespace PseudoMarkets.ReferenceData.TradingInstruments.Persistence.Repositories;

public sealed class TradingInstrumentRepository : ITradingInstrumentRepository
{
    private readonly PseudoMarketsDbContext _dbContext;

    public TradingInstrumentRepository(PseudoMarketsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(string symbol, CancellationToken cancellationToken)
    {
        return _dbContext.TradingInstruments.AnyAsync(x => x.Symbol == symbol, cancellationToken);
    }

    public async Task<TradingInstrument?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.TradingInstruments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Symbol == symbol, cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<TradingInstrument> AddAsync(TradingInstrument instrument, CancellationToken cancellationToken)
    {
        var entity = ToEntity(instrument);
        _dbContext.TradingInstruments.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToModel(entity);
    }

    public async Task<TradingInstrument> UpdateClosingPriceAsync(
        string symbol,
        double closingPrice,
        DateOnly closingPriceDate,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.TradingInstruments
            .SingleOrDefaultAsync(x => x.Symbol == symbol, cancellationToken);

        if (entity is null)
        {
            throw new TradingInstrumentNotFoundException($"Trading instrument '{symbol}' was not found.");
        }

        entity.ClosingPrice = closingPrice;
        entity.ClosingPriceDate = closingPriceDate;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToModel(entity);
    }

    private static TradingInstrument ToModel(TradingInstrumentEntity entity)
    {
        return new TradingInstrument
        {
            Symbol = entity.Symbol,
            Description = entity.Description,
            TradingStatus = entity.TradingStatus,
            PrimaryInstrumentType = entity.PrimaryInstrumentType,
            SecondaryInstrumentType = entity.SecondaryInstrumentType,
            ClosingPrice = entity.ClosingPrice,
            ClosingPriceDate = entity.ClosingPriceDate,
            Source = entity.Source
        };
    }

    private static TradingInstrumentEntity ToEntity(TradingInstrument instrument)
    {
        return new TradingInstrumentEntity
        {
            Symbol = instrument.Symbol,
            Description = instrument.Description,
            TradingStatus = instrument.TradingStatus,
            PrimaryInstrumentType = instrument.PrimaryInstrumentType,
            SecondaryInstrumentType = instrument.SecondaryInstrumentType,
            ClosingPrice = instrument.ClosingPrice,
            ClosingPriceDate = instrument.ClosingPriceDate,
            Source = instrument.Source
        };
    }
}
