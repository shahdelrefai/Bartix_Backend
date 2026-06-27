namespace Bartrix.Modules.Reports.Application;

public sealed class ReportsValidationException : Exception
{
    public ReportsValidationException(string message) : base(message) { }
}
