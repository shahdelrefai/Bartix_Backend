namespace Bartrix.Modules.Search.Application;

public sealed class SearchValidationException : Exception
{
    public SearchValidationException(string message) : base(message)
    {
    }
}
