using Minio;

namespace Bartrix.BuildingBlocks.Storage;

public interface IMinioClientFactory
{
    IMinioClient CreateClient();
}
