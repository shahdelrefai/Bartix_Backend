using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Listings.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Listings.Infrastructure;

public sealed class ListingsDbContext : DbContext
{
    public DbSet<Listing> Listings => Set<Listing>();

    public DbSet<ListingImage> ListingImages => Set<ListingImage>();

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
            builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Location).HasMaxLength(200).IsRequired();
            builder.Property(x => x.AskingPrice).HasColumnType("numeric(12,2)");
            builder.Property(x => x.Condition).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasMany(x => x.Images)
                .WithOne(x => x.Listing)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.OwnerUserId);
            builder.HasIndex(x => x.Category);
            builder.HasIndex(x => x.IsActive);
        });

        modelBuilder.Entity<ListingImage>(builder =>
        {
            builder.ToTable("listing_images");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Url).HasMaxLength(500).IsRequired();
            builder.Property(x => x.SortOrder).IsRequired();
        });
    }
}
