namespace Bartrix.Modules.Notifications.Application;

public sealed class NotificationValidationException : Exception
{
    public NotificationValidationException(string message) : base(message)
    {
    }
}
