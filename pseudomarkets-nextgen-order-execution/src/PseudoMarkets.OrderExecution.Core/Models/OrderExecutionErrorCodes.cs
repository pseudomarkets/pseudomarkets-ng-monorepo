namespace PseudoMarkets.OrderExecution.Core.Models;

public static class OrderExecutionErrorCodes
{
    public const string UnsupportedOrderType = "UNSUPPORTED_ORDER_TYPE";
    public const string InvalidSymbolFormat = "INVALID_SYMBOL_FORMAT";
    public const string UnsupportedSymbol = "UNSUPPORTED_SYMBOL";
    public const string SymbolNotTradable = "SYMBOL_NOT_TRADABLE";
    public const string UnsupportedInstrumentType = "UNSUPPORTED_INSTRUMENT_TYPE";
    public const string TradingInstrumentsUnavailable = "TRADING_INSTRUMENTS_UNAVAILABLE";
    public const string InvalidUser = "INVALID_USER";
    public const string UserOwnershipViolation = "USER_OWNERSHIP_VIOLATION";
    public const string InvalidQuantity = "INVALID_QUANTITY";
    public const string MarketDataUnavailable = "MARKET_DATA_UNAVAILABLE";
    public const string InvalidMarketPrice = "INVALID_MARKET_PRICE";
    public const string MissingBalance = "MISSING_BALANCE";
    public const string InsufficientSettledCash = "INSUFFICIENT_SETTLED_CASH";
    public const string MissingPosition = "MISSING_POSITION";
    public const string InsufficientSettledPosition = "INSUFFICIENT_SETTLED_POSITION";
    public const string SystemTokenUnavailable = "SYSTEM_TOKEN_UNAVAILABLE";
    public const string DownstreamUnauthorized = "DOWNSTREAM_UNAUTHORIZED";
    public const string TransactionPostingFailed = "TRANSACTION_POSTING_FAILED";
}
