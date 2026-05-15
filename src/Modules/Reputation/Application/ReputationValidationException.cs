namespace Bartrix.Modules.Reputation.Application;

public sealed class ReputationValidationException : Exception
{
    public ReputationValidationException(string message) : base(message)
    {
    }
}
