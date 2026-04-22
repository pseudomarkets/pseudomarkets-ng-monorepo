using PseudoMarkets.ReferenceData.TradingInstruments.Contracts.Instruments;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Exceptions;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Interfaces;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Models;

namespace PseudoMarkets.ReferenceData.TradingInstruments.Core.Services;

public sealed class TradingInstrumentService : ITradingInstrumentService
{
    private const int SymbolMaxLength = 32;
    private const int DescriptionMaxLength = 512;
    private const int TypeMaxLength = 50;
    private const int SourceMaxLength = 100;

    private readonly ITradingInstrumentRepository _repository;

    public TradingInstrumentService(ITradingInstrumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<TradingInstrumentResponse> GetBySymbolAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        var instrument = await _repository.GetBySymbolAsync(normalizedSymbol, cancellationToken);

        if (instrument is null)
        {
            throw new TradingInstrumentNotFoundException($"Trading instrument '{normalizedSymbol}' was not found.");
        }

        return ToResponse(instrument);
    }

    public async Task<TradingInstrumentResponse> CreateAsync(
        CreateTradingInstrumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var symbol = NormalizeSymbol(request.Symbol);
        ValidateText(request.Description, nameof(request.Description), DescriptionMaxLength);
        ValidateText(request.PrimaryInstrumentType, nameof(request.PrimaryInstrumentType), TypeMaxLength);
        ValidateText(request.SecondaryInstrumentType, nameof(request.SecondaryInstrumentType), TypeMaxLength);
        ValidateText(request.Source, nameof(request.Source), SourceMaxLength);
        ValidateClosingPrice(request.ClosingPrice);

        if (await _repository.ExistsAsync(symbol, cancellationToken))
        {
            throw new TradingInstrumentConflictException($"Trading instrument '{symbol}' already exists.");
        }

        var instrument = new TradingInstrument
        {
            Symbol = symbol,
            Description = request.Description.Trim(),
            TradingStatus = true,
            PrimaryInstrumentType = request.PrimaryInstrumentType.Trim(),
            SecondaryInstrumentType = request.SecondaryInstrumentType.Trim(),
            ClosingPrice = request.ClosingPrice,
            ClosingPriceDate = request.ClosingPriceDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Source = request.Source.Trim()
        };

        return ToResponse(await _repository.AddAsync(instrument, cancellationToken));
    }

    public async Task<TradingInstrumentResponse> UpdateClosingPriceAsync(
        string symbol,
        UpdateClosingPriceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedSymbol = NormalizeSymbol(symbol);
        ValidateClosingPrice(request.ClosingPrice);

        var closingPriceDate = request.ClosingPriceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var instrument = await _repository.UpdateClosingPriceAsync(
            normalizedSymbol,
            request.ClosingPrice,
            closingPriceDate,
            cancellationToken);

        return ToResponse(instrument);
    }

    private static string NormalizeSymbol(string symbol)
    {
        var normalizedSymbol = (symbol ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalizedSymbol))
        {
            throw new TradingInstrumentValidationException("Symbol is required.");
        }

        if (normalizedSymbol.Length > SymbolMaxLength)
        {
            throw new TradingInstrumentValidationException($"Symbol cannot exceed {SymbolMaxLength} characters.");
        }

        return normalizedSymbol;
    }

    private static void ValidateText(string value, string fieldName, int maxLength)
    {
        var trimmedValue = (value ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(trimmedValue))
        {
            throw new TradingInstrumentValidationException($"{fieldName} is required.");
        }

        if (trimmedValue.Length > maxLength)
        {
            throw new TradingInstrumentValidationException($"{fieldName} cannot exceed {maxLength} characters.");
        }
    }

    private static void ValidateClosingPrice(double closingPrice)
    {
        if (double.IsNaN(closingPrice) || double.IsInfinity(closingPrice) || closingPrice < 0)
        {
            throw new TradingInstrumentValidationException("Closing price must be greater than or equal to 0.");
        }
    }

    private static TradingInstrumentResponse ToResponse(TradingInstrument instrument)
    {
        return new TradingInstrumentResponse
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
