namespace Bartrix.Modules.Admin.Application;

public sealed class AdminValidationException : Exception
{
    public AdminValidationException(string message) : base(message) { }
}
