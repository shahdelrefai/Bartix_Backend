namespace Bartrix.Modules.Trades.Application;

public sealed class TradesValidationException : Exception
{
    public TradesValidationException(string message) : base(message)
    {
    }
}
