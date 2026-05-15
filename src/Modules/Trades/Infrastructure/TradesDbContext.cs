using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Trades.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Trades.Infrastructure;

public sealed class TradesDbContext : DbContext
{
    public DbSet<TradeProposal> TradeProposals => Set<TradeProposal>();

    public DbSet<TradeProposalOfferedListing> TradeProposalOfferedListings => Set<TradeProposalOfferedListing>();

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
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasMany(x => x.OfferedListings)
                .WithOne(x => x.TradeProposal)
                .HasForeignKey(x => x.TradeProposalId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.SenderUserId);
            builder.HasIndex(x => x.ReceiverUserId);
            builder.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<TradeProposalOfferedListing>(builder =>
        {
            builder.ToTable("trade_proposal_offered_listings");
            builder.HasKey(x => new { x.TradeProposalId, x.ListingId });
        });
    }
}
