using Microsoft.AspNetCore.Hosting;

namespace Bartrix.Api.Media;

public sealed class LocalMediaStorage : IMediaStorage
{
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalMediaStorage(
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor)
    {
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<StoredMedia> UploadAsync(
        Stream content,
        long length,
        string contentType,
        string fileExtension,
        CancellationToken cancellationToken)
    {
        var extension = string.IsNullOrWhiteSpace(fileExtension)
            ? string.Empty
            : fileExtension.StartsWith('.') ? fileExtension : "." + fileExtension;

        var relativeObjectName = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
        var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;
        var targetPath = Path.Combine(
            webRoot,
            "media",
            relativeObjectName.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        await using (var output = File.Create(targetPath))
        {
            await content.CopyToAsync(output, cancellationToken);
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = request is null
            ? string.Empty
            : $"{request.Scheme}://{request.Host}";
        var urlPath = $"/media/{relativeObjectName}";

        return new StoredMedia(relativeObjectName, $"{baseUrl}{urlPath}");
    }

    public Task DeleteAsync(string objectName, CancellationToken cancellationToken)
    {
        var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;
        var targetPath = Path.Combine(
            webRoot,
            "media",
            objectName.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        return Task.CompletedTask;
    }
}
