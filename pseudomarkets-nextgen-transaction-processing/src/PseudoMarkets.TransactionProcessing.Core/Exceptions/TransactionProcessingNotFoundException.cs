namespace PseudoMarkets.TransactionProcessing.Core.Exceptions;

public class TransactionProcessingNotFoundException : Exception
{
    public TransactionProcessingNotFoundException(string message)
        : base(message)
    {
    }
}
