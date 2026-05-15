namespace Bartrix.Modules.Auth.Application;

public sealed class AuthValidationException : Exception
{
    public AuthValidationException(string message) : base(message)
    {
    }
}
