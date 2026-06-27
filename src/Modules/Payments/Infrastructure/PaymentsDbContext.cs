using Bartrix.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Payments.Infrastructure;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("payments", "payments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.BuyerId).HasColumnName("buyer_id");
            e.Property(x => x.SellerId).HasColumnName("seller_id");
            e.Property(x => x.ProductTitle).HasColumnName("product_title").HasMaxLength(500);
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(14,2)");
            e.Property(x => x.FeeAmount).HasColumnName("fee_amount").HasColumnType("numeric(14,2)");
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
            e.Property(x => x.PaymobTransactionId).HasColumnName("paymob_transaction_id").HasMaxLength(200);
            e.Property(x => x.IsCredited).HasColumnName("is_credited");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        });
    }
}
