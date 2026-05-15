namespace Bartrix.Modules.Delivery.Application;

public sealed class DeliveryValidationException : Exception
{
    public DeliveryValidationException(string message) : base(message)
    {
    }
}
