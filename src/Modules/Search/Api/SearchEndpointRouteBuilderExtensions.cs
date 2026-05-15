using Bartrix.Modules.Search.Application;
using Bartrix.Modules.Search.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bartrix.Modules.Search.Api;

public static class SearchEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/search");
        group.WithTags("Search");
        group.AddEndpointFilter<SearchValidationFilter>();

        group.MapGet("/", async (
            [AsParameters] SearchQuery query,
            ISearchService searchService,
            CancellationToken cancellationToken) =>
        {
            var response = await searchService.SearchAsync(query, cancellationToken);
            return Results.Ok(response);
        });

        return app;
    }
}
