using Bartrix.Modules.Services.Contracts;
using Bartrix.Modules.Services.Domain;
using Bartrix.Modules.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Services.Application;

public sealed class ServicesService : IServicesService
{
    private readonly ServicesDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ServicesService(ServicesDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<ServiceOfferResponse> CreateAsync(Guid ownerUserId, CreateServiceOfferRequest request, CancellationToken cancellationToken)
    {
        var model = Normalize(
            request.Title,
            request.Description,
            request.Category,
            request.Location,
            request.FulfillmentMode,
            request.PricingType,
            request.PriceAmount,
            request.IsAvailableForTrade);

        var nowUtc = _timeProvider.GetUtcNow();
        var serviceOffer = new ServiceOffer(
            ownerUserId,
            model.Title,
            model.Description,
            model.Category,
            model.Location,
            model.FulfillmentMode,
            model.PricingType,
            model.PriceAmount,
            model.IsAvailableForTrade,
            nowUtc);

        _dbContext.ServiceOffers.Add(serviceOffer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(serviceOffer);
    }

    public async Task<ServiceOfferResponse> UpdateAsync(Guid ownerUserId, Guid serviceOfferId, UpdateServiceOfferRequest request, CancellationToken cancellationToken)
    {
        var serviceOffer = await _dbContext.ServiceOffers
            .SingleOrDefaultAsync(x => x.Id == serviceOfferId, cancellationToken);

        if (serviceOffer is null)
        {
            throw new ServicesValidationException("Service offer was not found.");
        }

        EnsureOwnership(ownerUserId, serviceOffer.OwnerUserId);

        var model = Normalize(
            request.Title,
            request.Description,
            request.Category,
            request.Location,
            request.FulfillmentMode,
            request.PricingType,
            request.PriceAmount,
            request.IsAvailableForTrade);

        serviceOffer.Update(
            model.Title,
            model.Description,
            model.Category,
            model.Location,
            model.FulfillmentMode,
            model.PricingType,
            model.PriceAmount,
            model.IsAvailableForTrade,
            _timeProvider.GetUtcNow());

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(serviceOffer);
    }

    public async Task ArchiveAsync(Guid ownerUserId, Guid serviceOfferId, CancellationToken cancellationToken)
    {
        var serviceOffer = await _dbContext.ServiceOffers
            .SingleOrDefaultAsync(x => x.Id == serviceOfferId, cancellationToken);

        if (serviceOffer is null)
        {
            throw new ServicesValidationException("Service offer was not found.");
        }

        EnsureOwnership(ownerUserId, serviceOffer.OwnerUserId);
        serviceOffer.Archive(_timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ServiceOfferResponse?> GetByIdAsync(Guid serviceOfferId, CancellationToken cancellationToken)
    {
        var serviceOffer = await _dbContext.ServiceOffers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == serviceOfferId, cancellationToken);

        return serviceOffer is null ? null : Map(serviceOffer);
    }

    public async Task<PagedServiceOffersResponse> BrowseAsync(ServicesQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page.GetValueOrDefault(1) <= 0 ? 1 : query.Page.GetValueOrDefault(1);
        var requestedPageSize = query.PageSize.GetValueOrDefault(20);
        var pageSize = requestedPageSize <= 0 ? 20 : Math.Min(requestedPageSize, 100);

        var serviceOffers = _dbContext.ServiceOffers
            .AsNoTracking()
            .AsQueryable();

        if (query.OnlyActive ?? true)
        {
            serviceOffers = serviceOffers.Where(x => x.IsActive);
        }

        if (query.OwnerUserId.HasValue)
        {
            serviceOffers = serviceOffers.Where(x => x.OwnerUserId == query.OwnerUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = query.Category.Trim().ToUpperInvariant();
            serviceOffers = serviceOffers.Where(x => x.Category.ToUpper() == category);
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var location = query.Location.Trim().ToUpperInvariant();
            serviceOffers = serviceOffers.Where(x => x.Location.ToUpper().Contains(location));
        }

        if (!string.IsNullOrWhiteSpace(query.FulfillmentMode) &&
            Enum.TryParse<ServiceFulfillmentMode>(query.FulfillmentMode, true, out var fulfillmentMode))
        {
            serviceOffers = serviceOffers.Where(x => x.FulfillmentMode == fulfillmentMode);
        }

        if (!string.IsNullOrWhiteSpace(query.PricingType) &&
            Enum.TryParse<ServicePricingType>(query.PricingType, true, out var pricingType))
        {
            serviceOffers = serviceOffers.Where(x => x.PricingType == pricingType);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();
            serviceOffers = serviceOffers.Where(x =>
                x.Title.ToUpper().Contains(search) ||
                x.Description.ToUpper().Contains(search));
        }

        var totalCount = await serviceOffers.CountAsync(cancellationToken);
        var items = await serviceOffers
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedServiceOffersResponse(
            items.Select(Map).ToList(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<IReadOnlyList<ServiceOfferResponse>> GetMineAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        var items = await _dbContext.ServiceOffers
            .AsNoTracking()
            .Where(x => x.OwnerUserId == ownerUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    private static void EnsureOwnership(Guid currentUserId, Guid ownerUserId)
    {
        if (currentUserId != ownerUserId)
        {
            throw new ServicesValidationException("You do not have permission to modify this service offer.");
        }
    }

    private static ServiceOfferWriteModel Normalize(
        string title,
        string description,
        string category,
        string location,
        string fulfillmentMode,
        string pricingType,
        decimal? priceAmount,
        bool isAvailableForTrade)
    {
        var normalizedTitle = NormalizeRequired(title, 200, "Title");
        var normalizedDescription = NormalizeRequired(description, 2000, "Description");
        var normalizedCategory = NormalizeRequired(category, 100, "Category");
        var normalizedLocation = NormalizeRequired(location, 200, "Location");

        if (!Enum.TryParse<ServiceFulfillmentMode>(fulfillmentMode, true, out var parsedFulfillmentMode))
        {
            throw new ServicesValidationException("Fulfillment mode must be 'Remote', 'OnSite', or 'Hybrid'.");
        }

        if (!Enum.TryParse<ServicePricingType>(pricingType, true, out var parsedPricingType))
        {
            throw new ServicesValidationException("Pricing type must be 'ExchangeOnly', 'FixedPrice', or 'Hourly'.");
        }

        if (priceAmount.HasValue && priceAmount.Value < 0)
        {
            throw new ServicesValidationException("Price amount cannot be negative.");
        }

        if (parsedPricingType == ServicePricingType.ExchangeOnly && priceAmount.HasValue)
        {
            throw new ServicesValidationException("Exchange-only services cannot define a price amount.");
        }

        if (parsedPricingType != ServicePricingType.ExchangeOnly && !priceAmount.HasValue)
        {
            throw new ServicesValidationException("Price amount is required for fixed-price or hourly services.");
        }

        if (!isAvailableForTrade && parsedPricingType == ServicePricingType.ExchangeOnly)
        {
            throw new ServicesValidationException("Exchange-only services must be available for trade.");
        }

        return new ServiceOfferWriteModel(
            normalizedTitle,
            normalizedDescription,
            normalizedCategory,
            normalizedLocation,
            parsedFulfillmentMode,
            parsedPricingType,
            priceAmount,
            isAvailableForTrade);
    }

    private static string NormalizeRequired(string value, int maxLength, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ServicesValidationException($"{fieldName} is required.");
        }

        if (normalized.Length > maxLength)
        {
            throw new ServicesValidationException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static ServiceOfferResponse Map(ServiceOffer serviceOffer)
    {
        return new ServiceOfferResponse(
            serviceOffer.Id,
            serviceOffer.OwnerUserId,
            serviceOffer.Title,
            serviceOffer.Description,
            serviceOffer.Category,
            serviceOffer.Location,
            serviceOffer.FulfillmentMode.ToString(),
            serviceOffer.PricingType.ToString(),
            serviceOffer.PriceAmount,
            serviceOffer.IsAvailableForTrade,
            serviceOffer.IsActive,
            serviceOffer.CreatedAtUtc,
            serviceOffer.UpdatedAtUtc);
    }

    private sealed record ServiceOfferWriteModel(
        string Title,
        string Description,
        string Category,
        string Location,
        ServiceFulfillmentMode FulfillmentMode,
        ServicePricingType PricingType,
        decimal? PriceAmount,
        bool IsAvailableForTrade);
}
