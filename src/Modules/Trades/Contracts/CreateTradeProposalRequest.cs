namespace Bartrix.Modules.Trades.Contracts;

public sealed record CreateTradeProposalRequest(
    Guid RequestedListingId,
    IReadOnlyList<Guid> OfferedListingIds,
    string? Message,
    // ─── Parity fields (all optional) ───────────────────────────────────
    IReadOnlyList<Guid>? RequestedListingIds = null,
    string Type = "any",
    string? SenderUserName = null,
    string? ReceiverUserName = null,
    bool IsFromPremium = false,
    int? ExpiresInHours = null);
