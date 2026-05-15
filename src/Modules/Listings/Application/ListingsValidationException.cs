namespace Bartrix.Modules.Listings.Application;

public sealed class ListingsValidationException : Exception
{
    public ListingsValidationException(string message) : base(message)
    {
    }
}
