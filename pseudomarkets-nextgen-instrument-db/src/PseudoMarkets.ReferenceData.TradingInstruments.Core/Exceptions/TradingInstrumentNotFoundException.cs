namespace PseudoMarkets.ReferenceData.TradingInstruments.Core.Exceptions;

public sealed class TradingInstrumentNotFoundException : Exception
{
    public TradingInstrumentNotFoundException(string message)
        : base(message)
    {
    }
}
