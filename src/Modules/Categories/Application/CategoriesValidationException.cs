namespace Bartrix.Modules.Categories.Application;

public sealed class CategoriesValidationException : Exception
{
    public CategoriesValidationException(string message) : base(message) { }
}
