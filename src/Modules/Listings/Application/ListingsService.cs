using Bartrix.Modules.Listings.Contracts;
using Bartrix.Modules.Listings.Domain;
using Bartrix.Modules.Listings.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Listings.Application;

public sealed class ListingsService : IListingsService
{
    private readonly ListingsDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ListingsService(ListingsDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<ListingResponse> CreateAsync(Guid ownerUserId, CreateListingRequest request, CancellationToken cancellationToken)
    {
        var model = Normalize(request.Title, request.Description, request.Category, request.Condition, request.Location,
            request.AskingPrice, request.IsAvailableForSwap, request.IsAvailableForSale, request.ImageUrls);
        var nowUtc = _timeProvider.GetUtcNow();

        var listing = new Listing(
            ownerUserId,
            model.Title,
            model.Description,
            model.Category,
            model.Condition,
            model.Location,
            model.AskingPrice,
            model.IsAvailableForSwap,
            model.IsAvailableForSale,
            nowUtc);

        listing.ReplaceImages(model.ImageUrls);

        _dbContext.Listings.Add(listing);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(listing);
    }

    public async Task<ListingResponse> UpdateAsync(Guid ownerUserId, Guid listingId, UpdateListingRequest request, CancellationToken cancellationToken)
    {
        var listing = await _dbContext.Listings
            .Include(x => x.Images)
            .SingleOrDefaultAsync(x => x.Id == listingId, cancellationToken);

        if (listing is null)
        {
            throw new ListingsValidationException("Listing was not found.");
        }

        EnsureOwnership(ownerUserId, listing.OwnerUserId);

        var model = Normalize(request.Title, request.Description, request.Category, request.Condition, request.Location,
            request.AskingPrice, request.IsAvailableForSwap, request.IsAvailableForSale, request.ImageUrls);

        listing.Update(
            model.Title,
            model.Description,
            model.Category,
            model.Condition,
            model.Location,
            model.AskingPrice,
            model.IsAvailableForSwap,
            model.IsAvailableForSale,
            _timeProvider.GetUtcNow());

        listing.ReplaceImages(model.ImageUrls);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(listing);
    }

    public async Task ArchiveAsync(Guid ownerUserId, Guid listingId, CancellationToken cancellationToken)
    {
        var listing = await _dbContext.Listings.SingleOrDefaultAsync(x => x.Id == listingId, cancellationToken);

        if (listing is null)
        {
            throw new ListingsValidationException("Listing was not found.");
        }

        EnsureOwnership(ownerUserId, listing.OwnerUserId);
        listing.Archive(_timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ListingResponse?> GetByIdAsync(Guid listingId, CancellationToken cancellationToken)
    {
        var listing = await _dbContext.Listings
            .AsNoTracking()
            .Include(x => x.Images)
            .SingleOrDefaultAsync(x => x.Id == listingId, cancellationToken);

        return listing is null ? null : Map(listing);
    }

    public async Task<PagedListingsResponse> BrowseAsync(ListingsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);

        var listings = _dbContext.Listings
            .AsNoTracking()
            .Include(x => x.Images)
            .AsQueryable();

        if (query.OnlyActive)
        {
            listings = listings.Where(x => x.IsActive);
        }

        if (query.OwnerUserId.HasValue)
        {
            listings = listings.Where(x => x.OwnerUserId == query.OwnerUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = query.Category.Trim().ToUpperInvariant();
            listings = listings.Where(x => x.Category.ToUpper() == category);
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var location = query.Location.Trim().ToUpperInvariant();
            listings = listings.Where(x => x.Location.ToUpper().Contains(location));
        }

        if (!string.IsNullOrWhiteSpace(query.Condition) &&
            Enum.TryParse<ListingCondition>(query.Condition, true, out var condition))
        {
            listings = listings.Where(x => x.Condition == condition);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();
            listings = listings.Where(x =>
                x.Title.ToUpper().Contains(search) ||
                x.Description.ToUpper().Contains(search));
        }

        var totalCount = await listings.CountAsync(cancellationToken);
        var items = await listings
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedListingsResponse(
            items.Select(Map).ToList(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<IReadOnlyList<ListingResponse>> GetMineAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        var items = await _dbContext.Listings
            .AsNoTracking()
            .Include(x => x.Images)
            .Where(x => x.OwnerUserId == ownerUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    private static void EnsureOwnership(Guid currentUserId, Guid ownerUserId)
    {
        if (currentUserId != ownerUserId)
        {
            throw new ListingsValidationException("You do not have permission to modify this listing.");
        }
    }

    private static ListingWriteModel Normalize(
        string title,
        string description,
        string category,
        string condition,
        string location,
        decimal? askingPrice,
        bool isAvailableForSwap,
        bool isAvailableForSale,
        IReadOnlyList<string>? imageUrls)
    {
        var normalizedTitle = NormalizeRequired(title, 200, "Title");
        var normalizedDescription = NormalizeRequired(description, 2000, "Description");
        var normalizedCategory = NormalizeRequired(category, 100, "Category");
        var normalizedLocation = NormalizeRequired(location, 200, "Location");

        if (!Enum.TryParse<ListingCondition>(condition, true, out var parsedCondition))
        {
            throw new ListingsValidationException("Condition must be 'New' or 'Used'.");
        }

        if (!isAvailableForSale && !isAvailableForSwap)
        {
            throw new ListingsValidationException("Listing must be available for sale, swap, or both.");
        }

        if (askingPrice.HasValue && askingPrice.Value < 0)
        {
            throw new ListingsValidationException("Asking price cannot be negative.");
        }

        if (isAvailableForSale && !askingPrice.HasValue)
        {
            throw new ListingsValidationException("Asking price is required when listing is available for sale.");
        }

        var normalizedImages = (imageUrls ?? Array.Empty<string>())
            .Select(url => NormalizeRequired(url, 500, "Image URL"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        return new ListingWriteModel(
            normalizedTitle,
            normalizedDescription,
            normalizedCategory,
            parsedCondition,
            normalizedLocation,
            askingPrice,
            isAvailableForSwap,
            isAvailableForSale,
            normalizedImages);
    }

    private static string NormalizeRequired(string value, int maxLength, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ListingsValidationException($"{fieldName} is required.");
        }

        if (normalized.Length > maxLength)
        {
            throw new ListingsValidationException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static ListingResponse Map(Listing listing)
    {
        return new ListingResponse(
            listing.Id,
            listing.OwnerUserId,
            listing.Title,
            listing.Description,
            listing.Category,
            listing.Condition.ToString(),
            listing.Location,
            listing.AskingPrice,
            listing.IsAvailableForSwap,
            listing.IsAvailableForSale,
            listing.IsActive,
            listing.CreatedAtUtc,
            listing.UpdatedAtUtc,
            listing.Images
                .OrderBy(x => x.SortOrder)
                .Select(x => new ListingImageResponse(x.Url, x.SortOrder))
                .ToList());
    }

    private sealed record ListingWriteModel(
        string Title,
        string Description,
        string Category,
        ListingCondition Condition,
        string Location,
        decimal? AskingPrice,
        bool IsAvailableForSwap,
        bool IsAvailableForSale,
        IReadOnlyList<string> ImageUrls);
}
