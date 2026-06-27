using Bartrix.BuildingBlocks.Storage;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using IMinioClientFactory = Bartrix.BuildingBlocks.Storage.IMinioClientFactory;

namespace Bartrix.Api.Media;

/// <summary>
/// Stores uploaded media in the configured MinIO bucket and returns a path-style
/// public URL. Replaces the client-side ImgBB / Firebase Storage uploads.
/// </summary>
public sealed class MinioMediaStorage : IMediaStorage
{
    private readonly IMinioClientFactory _clientFactory;
    private readonly MinioOptions _options;
    private bool _bucketChecked;

    public MinioMediaStorage(IMinioClientFactory clientFactory, IOptions<MinioOptions> options)
    {
        _clientFactory = clientFactory;
        _options = options.Value;
    }

    public async Task<StoredMedia> UploadAsync(
        Stream content,
        long length,
        string contentType,
        string fileExtension,
        CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient();
        await EnsureBucketAsync(client, cancellationToken);

        var normalizedExtension = string.IsNullOrWhiteSpace(fileExtension)
            ? string.Empty
            : (fileExtension.StartsWith('.') ? fileExtension : "." + fileExtension);

        var objectName = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{normalizedExtension}";

        var putArgs = new PutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectName)
            .WithStreamData(content)
            .WithObjectSize(length)
            .WithContentType(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

        await client.PutObjectAsync(putArgs, cancellationToken);

        return new StoredMedia(objectName, BuildPublicUrl(objectName));
    }

    public async Task DeleteAsync(string objectName, CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient();
        var removeArgs = new RemoveObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectName);

        await client.RemoveObjectAsync(removeArgs, cancellationToken);
    }

    private async Task EnsureBucketAsync(IMinioClient client, CancellationToken cancellationToken)
    {
        if (_bucketChecked || !_options.CreateBucketIfMissing)
        {
            _bucketChecked = true;
            return;
        }

        var exists = await client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_options.BucketName),
            cancellationToken);

        if (!exists)
        {
            await client.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_options.BucketName),
                cancellationToken);
        }

        _bucketChecked = true;
    }

    private string BuildPublicUrl(string objectName)
    {
        var scheme = _options.UseSsl ? "https" : "http";
        return $"{scheme}://{_options.Endpoint}/{_options.BucketName}/{objectName}";
    }
}
