using Bartrix.Modules.Search.Application;
using Bartrix.Modules.Search.Contracts;

namespace Bartrix.Modules.Search.Tests;

public sealed class SearchServiceTests
{
    [Fact]
    public async Task SearchAsync_ReturnsMappedResults()
    {
        var reader = new FakeSearchCatalogReader(new SearchCatalogPage(
            new[]
            {
                new SearchCatalogItem(
                    Guid.NewGuid(),
                    SearchSourceType.Listings,
                    Guid.NewGuid(),
                    "MacBook Pro",
                    "Used laptop",
                    "Electronics",
                    "Cairo",
                    25000m,
                    true,
                    "Used",
                    true,
                    null,
                    null,
                    new DateTimeOffset(2026, 5, 14, 10, 0, 0, TimeSpan.Zero))
            },
            1));

        var service = new SearchService(reader);

        var response = await service.SearchAsync(new SearchQuery(), CancellationToken.None);

        Assert.Single(response.Items);
        Assert.Equal("Listings", response.Items[0].Type);
        Assert.Equal(1, response.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_NormalizesPagingBounds()
    {
        var reader = new FakeSearchCatalogReader(new SearchCatalogPage(Array.Empty<SearchCatalogItem>(), 0));
        var service = new SearchService(reader);

        await service.SearchAsync(new SearchQuery { Page = 0, PageSize = 500 }, CancellationToken.None);

        Assert.NotNull(reader.LastRequest);
        Assert.Equal(1, reader.LastRequest!.Page);
        Assert.Equal(100, reader.LastRequest.PageSize);
    }

    [Fact]
    public async Task SearchAsync_RejectsInvalidType()
    {
        var reader = new FakeSearchCatalogReader(new SearchCatalogPage(Array.Empty<SearchCatalogItem>(), 0));
        var service = new SearchService(reader);

        await Assert.ThrowsAsync<SearchValidationException>(() =>
            service.SearchAsync(new SearchQuery { Type = "Unknown" }, CancellationToken.None));
    }

    [Fact]
    public async Task SearchAsync_ParsesServicesType()
    {
        var reader = new FakeSearchCatalogReader(new SearchCatalogPage(Array.Empty<SearchCatalogItem>(), 0));
        var service = new SearchService(reader);

        await service.SearchAsync(new SearchQuery { Type = "Services" }, CancellationToken.None);

        Assert.Equal(SearchSourceType.Services, reader.LastRequest!.SourceType);
    }

    private sealed class FakeSearchCatalogReader : ISearchCatalogReader
    {
        private readonly SearchCatalogPage _response;

        public FakeSearchCatalogReader(SearchCatalogPage response)
        {
            _response = response;
        }

        public SearchCatalogRequest? LastRequest { get; private set; }

        public Task<SearchCatalogPage> SearchAsync(SearchCatalogRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_response);
        }
    }
}
