namespace PseudoMarkets.ReferenceData.TradingInstruments.Core.Exceptions;

public sealed class TradingInstrumentConflictException : Exception
{
    public TradingInstrumentConflictException(string message)
        : base(message)
    {
    }
}
