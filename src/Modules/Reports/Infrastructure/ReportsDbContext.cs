using Bartrix.Modules.Reports.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Reports.Infrastructure;

public sealed class ReportsDbContext : DbContext
{
    public DbSet<Report> Reports => Set<Report>();

    public ReportsDbContext(DbContextOptions<ReportsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reports");

        modelBuilder.Entity<Report>(builder =>
        {
            builder.ToTable("reports");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ReporterName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.ReportedProductTitle).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Reason).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(2000);
            builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
            builder.Property(x => x.AdminNote).HasMaxLength(1000);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.ReporterId);
            builder.HasIndex(x => x.ReportedProductId);
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
