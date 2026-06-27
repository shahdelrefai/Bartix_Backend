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
            builder.Property(x => x.ReviewerName).HasMaxLength(200);
            builder.Property(x => x.Title).HasMaxLength(200);
            builder.Property(x => x.Comment).HasMaxLength(1000);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasIndex(x => new { x.TradeProposalId, x.ReviewerUserId })
                .IsUnique()
                .HasFilter("trade_proposal_id IS NOT NULL");
            builder.HasIndex(x => x.RevieweeUserId);
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
