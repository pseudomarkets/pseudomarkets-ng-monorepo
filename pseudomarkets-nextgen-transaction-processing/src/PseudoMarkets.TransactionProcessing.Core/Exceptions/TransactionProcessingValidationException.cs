namespace PseudoMarkets.TransactionProcessing.Core.Exceptions;

public class TransactionProcessingValidationException : Exception
{
    public TransactionProcessingValidationException(string message)
        : base(message)
    {
    }
}
