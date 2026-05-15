namespace Bartrix.Modules.Profiles.Application;

public sealed class ProfileValidationException : Exception
{
    public ProfileValidationException(string message) : base(message)
    {
    }
}
