namespace Bartrix.Modules.Trades.Domain;

public sealed class TradeProposalOfferedListing
{
    public Guid TradeProposalId { get; private set; }

    public Guid ListingId { get; private set; }

    public TradeProposal TradeProposal { get; private set; } = null!;

    private TradeProposalOfferedListing()
    {
    }

    public TradeProposalOfferedListing(Guid tradeProposalId, Guid listingId)
    {
        TradeProposalId = tradeProposalId;
        ListingId = listingId;
    }
}
