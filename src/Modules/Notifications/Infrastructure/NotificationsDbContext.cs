using Bartrix.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Notifications.Infrastructure;

public sealed class NotificationsDbContext : DbContext
{
    public DbSet<Notification> Notifications => Set<Notification>();

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");

        modelBuilder.Entity<Notification>(builder =>
        {
            builder.ToTable("notifications");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Body).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.Type).HasMaxLength(40).IsRequired();
            builder.Property(x => x.RelatedId).HasMaxLength(100);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasIndex(x => new { x.UserId, x.IsRead });
            builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
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
