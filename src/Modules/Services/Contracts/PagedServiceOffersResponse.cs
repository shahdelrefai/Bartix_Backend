namespace Bartrix.Modules.Services.Contracts;

public sealed record PagedServiceOffersResponse(
    IReadOnlyList<ServiceOfferResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
