using Bartrix.Modules.Categories.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Categories.Infrastructure;

public sealed class CategoriesDbContext : DbContext
{
    public DbSet<ApprovedCategory> ApprovedCategories => Set<ApprovedCategory>();
    public DbSet<CategorySuggestion> CategorySuggestions => Set<CategorySuggestion>();

    public CategoriesDbContext(DbContextOptions<CategoriesDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("categories");

        modelBuilder.Entity<ApprovedCategory>(builder =>
        {
            builder.ToTable("approved_categories");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
            builder.Property(x => x.AddedByName).HasMaxLength(200).IsRequired();
            builder.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<CategorySuggestion>(builder =>
        {
            builder.ToTable("category_suggestions");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SuggestedName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.SuggestedByName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
            builder.Property(x => x.ReviewedByName).HasMaxLength(200);
            builder.HasIndex(x => x.Status);
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
