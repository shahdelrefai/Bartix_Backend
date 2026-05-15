namespace Bartrix.Modules.Services.Application;

public sealed class ServicesValidationException : Exception
{
    public ServicesValidationException(string message) : base(message)
    {
    }
}
