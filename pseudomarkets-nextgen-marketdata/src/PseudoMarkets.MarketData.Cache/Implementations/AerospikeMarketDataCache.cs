using System.Globalization;
using Aerospike.Client;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using PseudoMarkets.MarketData.Contracts.Quotes;
using PseudoMarkets.MarketData.Core.Configuration;
using PseudoMarkets.MarketData.Core.Interfaces;

namespace PseudoMarkets.MarketData.Cache.Implementations;

public class AerospikeMarketDataCache : IMarketDataCache
{
    private const string NamespaceName = "nsPseudoMarkets";
    private const string QuoteSetName = "sMarketQuotes";
    private const string DetailedQuoteSetName = "sDetailedMarketQuotes";
    private const string IndicesSetName = "sMarketIndices";
    private const string SymbolBinName = "symbol";
    private const string PriceBinName = "price";
    private const string SourceBinName = "source";
    private const string TimestampUtcBinName = "timestampUtc";
    private const string NameBinName = "name";
    private const string OpenBinName = "open";
    private const string HighBinName = "high";
    private const string LowBinName = "low";
    private const string CloseBinName = "close";
    private const string VolumeBinName = "volume";
    private const string PreviousCloseBinName = "previousClose";
    private const string ChangeBinName = "change";
    private const string ChangePercentageBinName = "changePercentage";
    private const string IntervalBinName = "interval";
    private const string IndicesPayloadBinName = "indicesPayload";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IAerospikeClient? _aerospikeClient;
    private readonly MarketDataCacheConfiguration _cacheConfiguration;
    private readonly ILogger<AerospikeMarketDataCache> _logger;
    private readonly Policy _readPolicy = new Policy()
    {
        sendKey = true
    };

    public AerospikeMarketDataCache(
        AerospikeConfiguration aerospikeConfiguration,
        MarketDataCacheConfiguration cacheConfiguration,
        ILogger<AerospikeMarketDataCache> logger)
    {
        _cacheConfiguration = cacheConfiguration;
        _logger = logger;

        try
        {
            _aerospikeClient = new AerospikeClient(aerospikeConfiguration.Host, aerospikeConfiguration.Port);
        }
        catch (AerospikeException ex)
        {
            _logger.LogWarning(ex, "Unable to initialize the Aerospike cache client. Quote requests will continue without cache reads or writes.");
        }
    }

    public AerospikeMarketDataCache(
        IAerospikeClient aerospikeClient,
        AerospikeConfiguration aerospikeConfiguration,
        MarketDataCacheConfiguration cacheConfiguration,
        ILogger<AerospikeMarketDataCache> logger)
    {
        _aerospikeClient = aerospikeClient;
        _cacheConfiguration = cacheConfiguration;
        _logger = logger;
    }

    public Task<QuoteResponse?> GetLatestQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (_aerospikeClient is null)
        {
            return Task.FromResult<QuoteResponse?>(null);
        }

        try
        {
            var record = _aerospikeClient.Get(_readPolicy, CreateQuoteKey(symbol));
            if (record is null)
            {
                return Task.FromResult<QuoteResponse?>(null);
            }

            var priceAsString = record.GetString(PriceBinName);
            var source = record.GetString(SourceBinName) ?? string.Empty;
            var timestampAsString = record.GetString(TimestampUtcBinName);

            if (!decimal.TryParse(priceAsString, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            {
                return Task.FromResult<QuoteResponse?>(null);
            }

            if (!DateTimeOffset.TryParse(timestampAsString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestampUtc))
            {
                timestampUtc = DateTimeOffset.UtcNow;
            }

            QuoteResponse quote = new()
            {
                Symbol = record.GetString(SymbolBinName) ?? symbol,
                Price = price,
                Source = source,
                TimestampUtc = timestampUtc
            };

            return Task.FromResult<QuoteResponse?>(quote);
        }
        catch (AerospikeException ex)
        {
            _logger.LogWarning(ex, "Failed to read cached quote for {Symbol} from Aerospike.", symbol);
            return Task.FromResult<QuoteResponse?>(null);
        }
    }

    public Task SetLatestQuoteAsync(QuoteResponse quote, CancellationToken cancellationToken = default)
    {
        if (_aerospikeClient is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            var writePolicy = new WritePolicy
            {
                sendKey = true,
                expiration = _cacheConfiguration.QuoteTtlSeconds
            };

            _aerospikeClient.Put(
                writePolicy,
                CreateQuoteKey(quote.Symbol),
                new Bin(SymbolBinName, quote.Symbol),
                new Bin(PriceBinName, quote.Price.ToString(CultureInfo.InvariantCulture)),
                new Bin(SourceBinName, quote.Source),
                new Bin(TimestampUtcBinName, quote.TimestampUtc.ToString("O", CultureInfo.InvariantCulture)));
        }
        catch (AerospikeException ex)
        {
            _logger.LogWarning(ex, "Failed to cache quote for {Symbol} in Aerospike.", quote.Symbol);
        }

        return Task.CompletedTask;
    }

    public Task<DetailedQuoteResponse?> GetDetailedQuoteAsync(string symbol, string interval, CancellationToken cancellationToken = default)
    {
        if (_aerospikeClient is null)
        {
            return Task.FromResult<DetailedQuoteResponse?>(null);
        }

        try
        {
            var record = _aerospikeClient.Get(_readPolicy, CreateDetailedQuoteKey(symbol, interval));
            if (record is null)
            {
                return Task.FromResult<DetailedQuoteResponse?>(null);
            }

            var timestampAsString = record.GetString(TimestampUtcBinName);
            if (!DateTimeOffset.TryParse(timestampAsString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestampUtc))
            {
                timestampUtc = DateTimeOffset.UtcNow;
            }

            DetailedQuoteResponse response = new()
            {
                Symbol = record.GetString(SymbolBinName) ?? symbol,
                Name = record.GetString(NameBinName) ?? string.Empty,
                Open = ParseDecimal(record.GetString(OpenBinName)),
                High = ParseDecimal(record.GetString(HighBinName)),
                Low = ParseDecimal(record.GetString(LowBinName)),
                Close = ParseDecimal(record.GetString(CloseBinName)),
                Volume = record.GetLong(VolumeBinName),
                PreviousClose = ParseDecimal(record.GetString(PreviousCloseBinName)),
                Change = ParseDecimal(record.GetString(ChangeBinName)),
                ChangePercentage = ParseDecimal(record.GetString(ChangePercentageBinName)),
                Source = record.GetString(SourceBinName) ?? string.Empty,
                TimestampUtc = timestampUtc
            };

            return Task.FromResult<DetailedQuoteResponse?>(response);
        }
        catch (AerospikeException ex)
        {
            _logger.LogWarning(ex, "Failed to read cached detailed quote for {Symbol} from Aerospike.", symbol);
            return Task.FromResult<DetailedQuoteResponse?>(null);
        }
    }

    public Task SetDetailedQuoteAsync(DetailedQuoteResponse quote, string interval, CancellationToken cancellationToken = default)
    {
        if (_aerospikeClient is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            var writePolicy = new WritePolicy
            {
                sendKey = true,
                expiration = _cacheConfiguration.DetailedQuoteTtlSeconds
            };

            _aerospikeClient.Put(
                writePolicy,
                CreateDetailedQuoteKey(quote.Symbol, interval),
                new Bin(SymbolBinName, quote.Symbol),
                new Bin(NameBinName, quote.Name),
                new Bin(OpenBinName, quote.Open.ToString(CultureInfo.InvariantCulture)),
                new Bin(HighBinName, quote.High.ToString(CultureInfo.InvariantCulture)),
                new Bin(LowBinName, quote.Low.ToString(CultureInfo.InvariantCulture)),
                new Bin(CloseBinName, quote.Close.ToString(CultureInfo.InvariantCulture)),
                new Bin(VolumeBinName, quote.Volume),
                new Bin(PreviousCloseBinName, quote.PreviousClose.ToString(CultureInfo.InvariantCulture)),
                new Bin(ChangeBinName, quote.Change.ToString(CultureInfo.InvariantCulture)),
                new Bin(ChangePercentageBinName, quote.ChangePercentage.ToString(CultureInfo.InvariantCulture)),
                new Bin(IntervalBinName, interval),
                new Bin(SourceBinName, quote.Source),
                new Bin(TimestampUtcBinName, quote.TimestampUtc.ToString("O", CultureInfo.InvariantCulture)));
        }
        catch (AerospikeException ex)
        {
            _logger.LogWarning(ex, "Failed to cache detailed quote for {Symbol} in Aerospike.", quote.Symbol);
        }

        return Task.CompletedTask;
    }

    public Task<IndicesResponse?> GetUsMarketIndicesAsync(CancellationToken cancellationToken = default)
    {
        if (_aerospikeClient is null)
        {
            return Task.FromResult<IndicesResponse?>(null);
        }

        try
        {
            var record = _aerospikeClient.Get(_readPolicy, CreateIndicesKey());
            if (record is null)
            {
                return Task.FromResult<IndicesResponse?>(null);
            }

            var payload = record.GetString(IndicesPayloadBinName);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return Task.FromResult<IndicesResponse?>(null);
            }

            var indices = JsonSerializer.Deserialize<IndicesResponse>(payload, JsonSerializerOptions);
            return Task.FromResult(indices);
        }
        catch (AerospikeException ex)
        {
            _logger.LogWarning(ex, "Failed to read cached U.S. market indices from Aerospike.");
            return Task.FromResult<IndicesResponse?>(null);
        }
    }

    public Task SetUsMarketIndicesAsync(IndicesResponse indices, CancellationToken cancellationToken = default)
    {
        if (_aerospikeClient is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            var writePolicy = new WritePolicy
            {
                sendKey = true,
                expiration = _cacheConfiguration.IndicesTtlSeconds
            };

            var payload = JsonSerializer.Serialize(indices, JsonSerializerOptions);
            _aerospikeClient.Put(
                writePolicy,
                CreateIndicesKey(),
                new Bin(IndicesPayloadBinName, payload));
        }
        catch (AerospikeException ex)
        {
            _logger.LogWarning(ex, "Failed to cache U.S. market indices in Aerospike.");
        }

        return Task.CompletedTask;
    }

    private Key CreateQuoteKey(string symbol)
    {
        return new Key(NamespaceName, QuoteSetName, symbol.ToUpperInvariant());
    }

    private Key CreateDetailedQuoteKey(string symbol, string interval)
    {
        return new Key(NamespaceName, DetailedQuoteSetName, $"{symbol.ToUpperInvariant()}:{interval}");
    }

    private Key CreateIndicesKey()
    {
        return new Key(NamespaceName, IndicesSetName, "us-indices");
    }

    private static decimal ParseDecimal(string? value)
    {
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue)
            ? parsedValue
            : 0m;
    }
}
