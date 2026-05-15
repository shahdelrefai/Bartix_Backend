namespace Bartrix.Modules.Messaging.Application;

public sealed class MessagingValidationException : Exception
{
    public MessagingValidationException(string message) : base(message)
    {
    }
}
