namespace PseudoMarkets.ReferenceData.TradingInstruments.Core.Exceptions;

public sealed class TradingInstrumentValidationException : Exception
{
    public TradingInstrumentValidationException(string message)
        : base(message)
    {
    }
}
