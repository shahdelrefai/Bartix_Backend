using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Api.Media;

public static class MediaEndpointRouteBuilderExtensions
{
    private const long MaxUploadBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/media");
        group.WithTags("Media");
        group.RequireAuthorization();

        group.MapPost("/", [Authorize] async (
            IFormFile file,
            IMediaStorage storage,
            CancellationToken cancellationToken) =>
        {
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "A non-empty file is required." });
            }

            if (file.Length > MaxUploadBytes)
            {
                return Results.BadRequest(new { error = "File exceeds the 10 MB limit." });
            }

            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                return Results.BadRequest(new { error = $"Unsupported content type '{file.ContentType}'." });
            }

            var extension = Path.GetExtension(file.FileName);
            await using var stream = file.OpenReadStream();
            var stored = await storage.UploadAsync(stream, file.Length, file.ContentType, extension, cancellationToken);

            return Results.Ok(new { objectName = stored.ObjectName, url = stored.Url });
        })
        .DisableAntiforgery();

        return app;
    }
}
