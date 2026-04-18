namespace PseudoMarkets.Security.IdentityServer.Core.Exceptions;

public class IdentityDependencyException : Exception
{
    public IdentityDependencyException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
