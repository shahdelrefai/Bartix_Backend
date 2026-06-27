using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Trades.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Trades.Infrastructure;

public sealed class TradesDbContext : DbContext
{
    public DbSet<TradeProposal> TradeProposals => Set<TradeProposal>();

    public DbSet<TradeProposalOfferedListing> TradeProposalOfferedListings => Set<TradeProposalOfferedListing>();

    public DbSet<TradeCounterOffer> TradeCounterOffers => Set<TradeCounterOffer>();

    public DbSet<TradeHistoryEntry> TradeHistory => Set<TradeHistoryEntry>();

    public TradesDbContext(DbContextOptions<TradesDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchemas.Trades);

        modelBuilder.Entity<TradeProposal>(builder =>
        {
            builder.ToTable("trade_proposals");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Message).HasMaxLength(1000);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.SenderUserName).HasMaxLength(200);
            builder.Property(x => x.ReceiverUserName).HasMaxLength(200);
            builder.Property(x => x.Type).HasMaxLength(40).IsRequired();
            builder.Property(x => x.RejectionReason).HasMaxLength(500);
            builder.Property(x => x.RequestedListingIds).HasColumnType("uuid[]");
            builder.Property(x => x.DeliveryProvidedBy).HasColumnType("uuid[]");
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.Property(x => x.ExpiresAtUtc).IsRequired();
            builder.HasMany(x => x.OfferedListings)
                .WithOne(x => x.TradeProposal)
                .HasForeignKey(x => x.TradeProposalId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.SenderUserId);
            builder.HasIndex(x => x.ReceiverUserId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.ParentTradeId);
        });

        modelBuilder.Entity<TradeProposalOfferedListing>(builder =>
        {
            builder.ToTable("trade_proposal_offered_listings");
            builder.HasKey(x => new { x.TradeProposalId, x.ListingId });
        });

        modelBuilder.Entity<TradeCounterOffer>(builder =>
        {
            builder.ToTable("trade_counter_offers");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Message).HasMaxLength(1000);
            builder.Property(x => x.OfferedListingIds).HasColumnType("uuid[]");
            builder.Property(x => x.RequestedListingIds).HasColumnType("uuid[]");
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasIndex(x => x.TradeProposalId);
        });

        modelBuilder.Entity<TradeHistoryEntry>(builder =>
        {
            builder.ToTable("trade_history");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
            builder.Property(x => x.PerformedByUserName).HasMaxLength(200);
            builder.Property(x => x.Details).HasMaxLength(2000);
            builder.Property(x => x.TimestampUtc).IsRequired();
            builder.HasIndex(x => x.TradeProposalId);
        });
    
        ApplySnakeCaseColumnNames(modelBuilder);
}
    private static void ApplySnakeCaseColumnNames(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
                property.SetColumnName(string.Concat(
                    property.Name.Select((c, i) =>
                        i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString())));
    }

}
