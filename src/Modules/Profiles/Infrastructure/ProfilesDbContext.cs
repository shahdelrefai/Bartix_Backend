using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Profiles.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Profiles.Infrastructure;

public sealed class ProfilesDbContext : DbContext
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public ProfilesDbContext(DbContextOptions<ProfilesDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchemas.Profiles);

        modelBuilder.Entity<UserProfile>(builder =>
        {
            builder.ToTable("user_profiles");
            builder.HasKey(x => x.UserId);
            builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Bio).HasMaxLength(1000);
            builder.Property(x => x.Location).HasMaxLength(200);
            builder.Property(x => x.AvatarUrl).HasMaxLength(500);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
        });
    }
}
