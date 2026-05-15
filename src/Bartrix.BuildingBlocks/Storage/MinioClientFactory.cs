using Minio;

namespace Bartrix.BuildingBlocks.Storage;

public sealed class MinioClientFactory : IMinioClientFactory
{
    private readonly Lazy<IMinioClient> _client;

    public MinioClientFactory(MinioOptions options)
    {
        _client = new Lazy<IMinioClient>(() =>
            new MinioClient()
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey)
                .WithSSL(options.UseSsl)
                .Build());
    }

    public IMinioClient CreateClient()
    {
        return _client.Value;
    }
}
