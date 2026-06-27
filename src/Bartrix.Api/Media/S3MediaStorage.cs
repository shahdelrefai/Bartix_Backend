using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Bartrix.Api.Media;

public sealed class S3MediaStorage : IMediaStorage
{
    private readonly S3Options _options;
    private readonly IAmazonS3 _client;
    private bool _bucketChecked;

    public S3MediaStorage(IOptions<S3Options> options)
    {
        _options = options.Value;

        var config = new AmazonS3Config { ForcePathStyle = _options.ForcePathStyle };

        if (!string.IsNullOrEmpty(_options.ServiceUrl))
            config.ServiceURL = _options.ServiceUrl;
        else
            config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_options.Region);

        _client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
    }

    public async Task<StoredMedia> UploadAsync(
        Stream content,
        long length,
        string contentType,
        string fileExtension,
        CancellationToken cancellationToken)
    {
        await EnsureBucketAsync(cancellationToken);

        var ext = string.IsNullOrWhiteSpace(fileExtension) ? string.Empty
            : (fileExtension.StartsWith('.') ? fileExtension : "." + fileExtension);
        var key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{ext}";

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
        }, cancellationToken);

        return new StoredMedia(key, BuildPublicUrl(key));
    }

    public async Task DeleteAsync(string objectName, CancellationToken cancellationToken)
    {
        await _client.DeleteObjectAsync(_options.BucketName, objectName, cancellationToken);
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketChecked || !_options.CreateBucketIfMissing)
        {
            _bucketChecked = true;
            return;
        }

        try
        {
            await _client.GetBucketLocationAsync(_options.BucketName, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await _client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = _options.BucketName,
                UseClientRegion = true,
            }, cancellationToken);
        }

        _bucketChecked = true;
    }

    private string BuildPublicUrl(string key)
    {
        if (!string.IsNullOrEmpty(_options.ServiceUrl))
            return $"{_options.ServiceUrl.TrimEnd('/')}/{_options.BucketName}/{key}";

        return $"https://{_options.BucketName}.s3.{_options.Region}.amazonaws.com/{key}";
    }
}
