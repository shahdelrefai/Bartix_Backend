namespace Bartrix.Api.Media;

public sealed class S3Options
{
    public const string SectionName = "Storage:S3";

    public string BucketName { get; init; } = "bartrix-media";
    public string Region { get; init; } = "us-east-1";
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;

    // Leave empty for real AWS. Set to "http://localhost:9000" for local MinIO.
    public string? ServiceUrl { get; init; }

    // Must be true when using MinIO (path-style URLs).
    public bool ForcePathStyle { get; init; }

    public bool CreateBucketIfMissing { get; init; }
}
