namespace Bartrix.Modules.Notifications.Application;

/// <summary>Resolves a recipient's preferred language to localize notifications.</summary>
public interface IUserLanguageReader
{
    Task<string> GetLanguageCodeAsync(Guid userId, CancellationToken cancellationToken);
}
