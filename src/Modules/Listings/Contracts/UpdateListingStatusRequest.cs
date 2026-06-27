namespace Bartrix.Modules.Listings.Contracts;

/// <summary>Owner-driven status change (available | traded | reserved | unavailable).</summary>
public sealed record UpdateListingStatusRequest(string Status);
