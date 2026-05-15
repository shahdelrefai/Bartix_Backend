using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Services.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Services.Infrastructure;

public sealed class ServicesDbContext : DbContext
{
    public DbSet<ServiceOffer> ServiceOffers => Set<ServiceOffer>();

    public ServicesDbContext(DbContextOptions<ServicesDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchemas.Services);

        modelBuilder.Entity<ServiceOffer>(builder =>
        {
            builder.ToTable("service_offers");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Location).HasMaxLength(200).IsRequired();
            builder.Property(x => x.FulfillmentMode).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.PricingType).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.PriceAmount).HasColumnType("numeric(12,2)");
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasIndex(x => x.OwnerUserId);
            builder.HasIndex(x => x.Category);
            builder.HasIndex(x => x.IsActive);
        });
    }
}
