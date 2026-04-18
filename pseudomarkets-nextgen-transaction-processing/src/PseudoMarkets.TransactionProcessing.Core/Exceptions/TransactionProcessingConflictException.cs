namespace PseudoMarkets.TransactionProcessing.Core.Exceptions;

public class TransactionProcessingConflictException : Exception
{
    public TransactionProcessingConflictException(string message)
        : base(message)
    {
    }
}
