using Bartrix.Modules.Listings.Contracts;
using Bartrix.Modules.Listings.Domain;
using Bartrix.Modules.Listings.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Listings.Application;

public sealed class ListingsService : IListingsService
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "available", "traded", "reserved", "unavailable"
    };

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

        ApplyProductDetails(listing, request.OwnerName, request.Type, request.TransactionType, request.Price,
            request.DesiredSwapCategory, request.CustomCategory, request.Latitude, request.Longitude, request.Tags,
            request.IsOwnerPremium, request.ServiceCategory, request.CustomServiceCategory, request.EstimatedDuration,
            request.PriceRange, request.AvailabilitySchedule, request.Skills);

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

        ApplyProductDetails(listing, request.OwnerName, request.Type, request.TransactionType, request.Price,
            request.DesiredSwapCategory, request.CustomCategory, request.Latitude, request.Longitude, request.Tags,
            request.IsOwnerPremium, request.ServiceCategory, request.CustomServiceCategory, request.EstimatedDuration,
            request.PriceRange, request.AvailabilitySchedule, request.Skills);

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

        if (listing is null)
        {
            return null;
        }

        var interested = await _dbContext.ListingFavorites.AsNoTracking()
            .Where(x => x.ListingId == listingId).Select(x => x.UserId).ToListAsync(cancellationToken);
        var viewed = await _dbContext.ListingViews.AsNoTracking()
            .Where(x => x.ListingId == listingId).Select(x => x.UserId).ToListAsync(cancellationToken);
        var reported = await _dbContext.ListingReports.AsNoTracking()
            .Where(x => x.ListingId == listingId).Select(x => x.UserId).ToListAsync(cancellationToken);

        return Map(listing, interested, viewed, reported);
    }

    public async Task<PagedListingsResponse> BrowseAsync(ListingsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page.GetValueOrDefault(1) <= 0 ? 1 : query.Page.GetValueOrDefault(1);
        var requestedPageSize = query.PageSize.GetValueOrDefault(20);
        var pageSize = requestedPageSize <= 0 ? 20 : Math.Min(requestedPageSize, 100);

        var listings = _dbContext.Listings
            .AsNoTracking()
            .Include(x => x.Images)
            .AsQueryable();

        if (query.OnlyActive ?? true)
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

        if (!string.IsNullOrWhiteSpace(query.Condition))
        {
            var condition = query.Condition.Trim().ToUpperInvariant();
            listings = listings.Where(x => x.Condition.ToUpper() == condition);
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
            items.Select(x => Map(x)).ToList(),
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

        return items.Select(x => Map(x)).ToList();
    }

    public async Task<IReadOnlyList<ListingResponse>> GetByIdsAsync(IReadOnlyList<Guid> listingIds, CancellationToken cancellationToken)
    {
        var ids = listingIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Array.Empty<ListingResponse>();
        }

        var items = await _dbContext.Listings
            .AsNoTracking()
            .Include(x => x.Images)
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        return items.Select(x => Map(x)).ToList();
    }

    public async Task<ListingResponse> UpdateStatusAsync(Guid ownerUserId, Guid listingId, UpdateListingStatusRequest request, CancellationToken cancellationToken)
    {
        var status = request.Status?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!AllowedStatuses.Contains(status))
        {
            throw new ListingsValidationException("Status must be available, traded, reserved, or unavailable.");
        }

        var listing = await _dbContext.Listings
            .Include(x => x.Images)
            .SingleOrDefaultAsync(x => x.Id == listingId, cancellationToken)
            ?? throw new ListingsValidationException("Listing was not found.");

        EnsureOwnership(ownerUserId, listing.OwnerUserId);
        listing.SetStatus(status, _timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(listing);
    }

    public async Task<FavouriteStateResponse> ToggleFavouriteAsync(Guid userId, Guid listingId, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.ListingFavorites
            .SingleOrDefaultAsync(x => x.UserId == userId && x.ListingId == listingId, cancellationToken);

        if (existing is null)
        {
            await EnsureListingExistsAsync(listingId, cancellationToken);
            _dbContext.ListingFavorites.Add(new ListingFavorite(userId, listingId, _timeProvider.GetUtcNow()));
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new FavouriteStateResponse(listingId, true);
        }

        _dbContext.ListingFavorites.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new FavouriteStateResponse(listingId, false);
    }

    public async Task AddFavouriteAsync(Guid userId, Guid listingId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.ListingFavorites
            .AnyAsync(x => x.UserId == userId && x.ListingId == listingId, cancellationToken);

        if (exists)
        {
            return;
        }

        await EnsureListingExistsAsync(listingId, cancellationToken);
        _dbContext.ListingFavorites.Add(new ListingFavorite(userId, listingId, _timeProvider.GetUtcNow()));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFavouriteAsync(Guid userId, Guid listingId, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.ListingFavorites
            .SingleOrDefaultAsync(x => x.UserId == userId && x.ListingId == listingId, cancellationToken);

        if (existing is null)
        {
            return;
        }

        _dbContext.ListingFavorites.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsFavouriteAsync(Guid userId, Guid listingId, CancellationToken cancellationToken)
    {
        return _dbContext.ListingFavorites
            .AnyAsync(x => x.UserId == userId && x.ListingId == listingId, cancellationToken);
    }

    public async Task<IReadOnlyList<ListingResponse>> GetFavouritesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var listingIds = await _dbContext.ListingFavorites.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.ListingId)
            .ToListAsync(cancellationToken);

        return await GetByIdsAsync(listingIds, cancellationToken);
    }

    public async Task IncrementViewAsync(Guid listingId, Guid userId, CancellationToken cancellationToken)
    {
        var listing = await _dbContext.Listings
            .SingleOrDefaultAsync(x => x.Id == listingId, cancellationToken);

        if (listing is null)
        {
            throw new ListingsValidationException("Listing was not found.");
        }

        var alreadyViewed = await _dbContext.ListingViews
            .AnyAsync(x => x.ListingId == listingId && x.UserId == userId, cancellationToken);

        if (alreadyViewed)
        {
            return;
        }

        _dbContext.ListingViews.Add(new ListingView(listingId, userId, _timeProvider.GetUtcNow()));
        listing.IncrementViewCount();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReportAsync(Guid listingId, Guid userId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.ListingReports
            .AnyAsync(x => x.ListingId == listingId && x.UserId == userId, cancellationToken);

        if (exists)
        {
            return;
        }

        await EnsureListingExistsAsync(listingId, cancellationToken);
        _dbContext.ListingReports.Add(new ListingReport(listingId, userId, _timeProvider.GetUtcNow()));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureListingExistsAsync(Guid listingId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Listings.AnyAsync(x => x.Id == listingId, cancellationToken);
        if (!exists)
        {
            throw new ListingsValidationException("Listing was not found.");
        }
    }

    private static void ApplyProductDetails(
        Listing listing,
        string? ownerName,
        string type,
        string transactionType,
        decimal? price,
        string? desiredSwapCategory,
        string? customCategory,
        double? latitude,
        double? longitude,
        IReadOnlyList<string>? tags,
        bool isOwnerPremium,
        string? serviceCategory,
        string? customServiceCategory,
        int? estimatedDuration,
        decimal? priceRange,
        string? availabilitySchedule,
        IReadOnlyList<string>? skills)
    {
        listing.SetProductDetails(
            string.IsNullOrWhiteSpace(ownerName) ? null : ownerName.Trim(),
            type,
            transactionType,
            price,
            string.IsNullOrWhiteSpace(desiredSwapCategory) ? null : desiredSwapCategory.Trim(),
            string.IsNullOrWhiteSpace(customCategory) ? null : customCategory.Trim(),
            latitude,
            longitude,
            tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct().ToList(),
            isOwnerPremium,
            string.IsNullOrWhiteSpace(serviceCategory) ? null : serviceCategory.Trim(),
            string.IsNullOrWhiteSpace(customServiceCategory) ? null : customServiceCategory.Trim(),
            estimatedDuration,
            priceRange,
            string.IsNullOrWhiteSpace(availabilitySchedule) ? null : availabilitySchedule.Trim(),
            skills?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct().ToList());
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
        var normalizedCondition = NormalizeRequired(condition, 20, "Condition");

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
            normalizedCondition,
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

    private static ListingResponse Map(
        Listing listing,
        IReadOnlyList<Guid>? interestedUserIds = null,
        IReadOnlyList<Guid>? viewedUserIds = null,
        IReadOnlyList<Guid>? reportedByUserIds = null)
    {
        return new ListingResponse(
            listing.Id,
            listing.OwnerUserId,
            listing.Title,
            listing.Description,
            listing.Category,
            listing.Condition,
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
                .ToList(),
            OwnerName: listing.OwnerName,
            Type: listing.Type,
            Status: listing.Status,
            TransactionType: listing.TransactionType,
            Price: listing.Price,
            DesiredSwapCategory: listing.DesiredSwapCategory,
            CustomCategory: listing.CustomCategory,
            Latitude: listing.Latitude,
            Longitude: listing.Longitude,
            Tags: listing.Tags.ToList(),
            ViewCount: listing.ViewCount,
            IsOwnerPremium: listing.IsOwnerPremium,
            ServiceCategory: listing.ServiceCategory,
            CustomServiceCategory: listing.CustomServiceCategory,
            EstimatedDuration: listing.EstimatedDuration,
            PriceRange: listing.PriceRange,
            AvailabilitySchedule: listing.AvailabilitySchedule,
            Skills: listing.Skills.ToList(),
            InterestedUserIds: interestedUserIds ?? Array.Empty<Guid>(),
            ViewedUserIds: viewedUserIds ?? Array.Empty<Guid>(),
            ReportedByUserIds: reportedByUserIds ?? Array.Empty<Guid>());
    }

    private sealed record ListingWriteModel(
        string Title,
        string Description,
        string Category,
        string Condition,
        string Location,
        decimal? AskingPrice,
        bool IsAvailableForSwap,
        bool IsAvailableForSale,
        IReadOnlyList<string> ImageUrls);
}
