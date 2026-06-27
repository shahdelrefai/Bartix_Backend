namespace Bartrix.Api.Media;

public sealed record StoredMedia(string ObjectName, string Url);

public interface IMediaStorage
{
    Task<StoredMedia> UploadAsync(
        Stream content,
        long length,
        string contentType,
        string fileExtension,
        CancellationToken cancellationToken);

    Task DeleteAsync(string objectName, CancellationToken cancellationToken);
}
