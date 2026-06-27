using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Listings.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Listings.Infrastructure;

public sealed class ListingsDbContext : DbContext
{
    public DbSet<Listing> Listings => Set<Listing>();

    public DbSet<ListingImage> ListingImages => Set<ListingImage>();

    public DbSet<ListingFavorite> ListingFavorites => Set<ListingFavorite>();

    public DbSet<ListingView> ListingViews => Set<ListingView>();

    public DbSet<ListingReport> ListingReports => Set<ListingReport>();

    public ListingsDbContext(DbContextOptions<ListingsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchemas.Listings);

        modelBuilder.Entity<Listing>(builder =>
        {
            builder.ToTable("listings");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.OwnerName).HasMaxLength(200);
            builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Location).HasMaxLength(200).IsRequired();
            builder.Property(x => x.AskingPrice).HasColumnType("numeric(12,2)");
            builder.Property(x => x.Condition).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Type).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
            builder.Property(x => x.TransactionType).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Price).HasColumnType("numeric(12,2)");
            builder.Property(x => x.DesiredSwapCategory).HasMaxLength(100);
            builder.Property(x => x.CustomCategory).HasMaxLength(100);
            builder.Property(x => x.ServiceCategory).HasMaxLength(100);
            builder.Property(x => x.CustomServiceCategory).HasMaxLength(100);
            builder.Property(x => x.PriceRange).HasColumnType("numeric(12,2)");
            builder.Property(x => x.AvailabilitySchedule).HasMaxLength(200);
            builder.Property(x => x.Tags).HasColumnType("text[]");
            builder.Property(x => x.Skills).HasColumnType("text[]");
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasMany(x => x.Images)
                .WithOne(x => x.Listing)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.OwnerUserId);
            builder.HasIndex(x => x.Category);
            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<ListingImage>(builder =>
        {
            builder.ToTable("listing_images");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Url).HasMaxLength(500).IsRequired();
            builder.Property(x => x.SortOrder).IsRequired();
        });

        modelBuilder.Entity<ListingFavorite>(builder =>
        {
            builder.ToTable("listing_favorites");
            builder.HasKey(x => new { x.UserId, x.ListingId });
            builder.HasIndex(x => x.ListingId);
        });

        modelBuilder.Entity<ListingView>(builder =>
        {
            builder.ToTable("listing_views");
            builder.HasKey(x => new { x.ListingId, x.UserId });
        });

        modelBuilder.Entity<ListingReport>(builder =>
        {
            builder.ToTable("listing_reports");
            builder.HasKey(x => new { x.ListingId, x.UserId });
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
