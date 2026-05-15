namespace Bartrix.BuildingBlocks.Storage;

public sealed class MinioOptions
{
    public const string SectionName = "Storage:Minio";

    public string Endpoint { get; init; } = "localhost:9000";

    public string AccessKey { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public string BucketName { get; init; } = "bartrix-media";

    public bool UseSsl { get; init; }

    public bool CreateBucketIfMissing { get; init; }
}
