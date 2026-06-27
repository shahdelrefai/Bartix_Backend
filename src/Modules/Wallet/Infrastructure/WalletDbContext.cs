using Bartrix.Modules.Wallet.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Wallet.Infrastructure;

public sealed class WalletDbContext(DbContextOptions<WalletDbContext> options) : DbContext(options)
{
    public DbSet<WalletTransaction> Transactions => Set<WalletTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WalletTransaction>(e =>
        {
            e.ToTable("transactions", "wallet");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(14,2)");
            e.Property(x => x.Type).HasColumnName("type").HasMaxLength(10);
            e.Property(x => x.ReferenceId).HasColumnName("reference_id").HasMaxLength(200);
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        });
    }
}
