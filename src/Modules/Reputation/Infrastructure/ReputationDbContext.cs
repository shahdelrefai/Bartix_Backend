using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Reputation.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Reputation.Infrastructure;

public sealed class ReputationDbContext : DbContext
{
    public DbSet<ReputationReview> Reviews => Set<ReputationReview>();

    public ReputationDbContext(DbContextOptions<ReputationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchemas.Reputation);

        modelBuilder.Entity<ReputationReview>(builder =>
        {
            builder.ToTable("reputation_reviews");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Comment).HasMaxLength(1000);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasIndex(x => new { x.TradeProposalId, x.ReviewerUserId }).IsUnique();
            builder.HasIndex(x => x.RevieweeUserId);
        });
    }
}
