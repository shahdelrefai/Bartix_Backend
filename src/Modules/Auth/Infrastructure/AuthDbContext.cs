using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Auth.Infrastructure;

public sealed class AuthDbContext : DbContext
{
    public DbSet<UserAccount> Users => Set<UserAccount>();

    public DbSet<RefreshTokenSession> RefreshTokenSessions => Set<RefreshTokenSession>();

    public DbSet<PhoneOtpChallenge> PhoneOtpChallenges => Set<PhoneOtpChallenge>();

    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchemas.Auth);

        modelBuilder.Entity<UserAccount>(builder =>
        {
            builder.ToTable("user_accounts");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
            builder.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
            builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.PhoneNumber).HasMaxLength(32);
            builder.Property(x => x.NormalizedPhoneNumber).HasMaxLength(32);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasIndex(x => x.NormalizedEmail).IsUnique();
            builder.HasIndex(x => x.NormalizedPhoneNumber).IsUnique();
        });

        modelBuilder.Entity<RefreshTokenSession>(builder =>
        {
            builder.ToTable("refresh_token_sessions");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.ExpiresAtUtc).IsRequired();
            builder.HasIndex(x => x.TokenHash).IsUnique();
            builder.HasOne(x => x.UserAccount)
                .WithMany()
                .HasForeignKey(x => x.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PhoneOtpChallenge>(builder =>
        {
            builder.ToTable("phone_otp_challenges");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.PhoneNumber).HasMaxLength(32).IsRequired();
            builder.Property(x => x.NormalizedPhoneNumber).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Purpose).HasMaxLength(64).IsRequired();
            builder.Property(x => x.CodeHash).HasMaxLength(512).IsRequired();
            builder.Property(x => x.ProviderChallengeId).HasMaxLength(128);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.ExpiresAtUtc).IsRequired();
            builder.HasIndex(x => new { x.NormalizedPhoneNumber, x.Purpose });
        });
    }
}
