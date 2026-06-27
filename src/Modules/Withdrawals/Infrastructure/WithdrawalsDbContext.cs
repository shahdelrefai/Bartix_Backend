using Bartrix.Modules.Withdrawals.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Withdrawals.Infrastructure;

public sealed class WithdrawalsDbContext(DbContextOptions<WithdrawalsDbContext> options) : DbContext(options)
{
    public DbSet<WithdrawalRequest> WithdrawalRequests => Set<WithdrawalRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WithdrawalRequest>(e =>
        {
            e.ToTable("withdrawal_requests", "withdrawals");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SellerId).HasColumnName("seller_id");
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(14,2)");
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
            e.Property(x => x.BankDetails).HasColumnName("bank_details").HasMaxLength(2000);
            e.Property(x => x.AdminNote).HasColumnName("admin_note").HasMaxLength(500);
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        });
    }
}
