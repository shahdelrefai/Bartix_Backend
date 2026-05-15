using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Delivery.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Delivery.Infrastructure;

public sealed class DeliveryDbContext : DbContext
{
    public DbSet<TradeDelivery> Deliveries => Set<TradeDelivery>();

    public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchemas.Delivery);

        modelBuilder.Entity<TradeDelivery>(builder =>
        {
            builder.ToTable("trade_deliveries");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Method).HasConversion<string>().HasMaxLength(30);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(x => x.Location).HasMaxLength(500);
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasIndex(x => x.TradeProposalId).IsUnique();
        });
    }
}
