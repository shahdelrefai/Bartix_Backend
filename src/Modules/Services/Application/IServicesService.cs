using Bartrix.Modules.Services.Contracts;

namespace Bartrix.Modules.Services.Application;

public interface IServicesService
{
    Task<ServiceOfferResponse> CreateAsync(Guid ownerUserId, CreateServiceOfferRequest request, CancellationToken cancellationToken);

    Task<ServiceOfferResponse> UpdateAsync(Guid ownerUserId, Guid serviceOfferId, UpdateServiceOfferRequest request, CancellationToken cancellationToken);

    Task ArchiveAsync(Guid ownerUserId, Guid serviceOfferId, CancellationToken cancellationToken);

    Task<ServiceOfferResponse?> GetByIdAsync(Guid serviceOfferId, CancellationToken cancellationToken);

    Task<PagedServiceOffersResponse> BrowseAsync(ServicesQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyList<ServiceOfferResponse>> GetMineAsync(Guid ownerUserId, CancellationToken cancellationToken);
}
