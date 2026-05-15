using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Messaging.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Messaging.Infrastructure;

public sealed class MessagingDbContext : DbContext
{
    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationMessage> Messages => Set<ConversationMessage>();

    public MessagingDbContext(DbContextOptions<MessagingDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchemas.Messaging);

        modelBuilder.Entity<Conversation>(builder =>
        {
            builder.ToTable("conversations");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasMany(x => x.Messages)
                .WithOne(x => x.Conversation)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.TradeProposalId).IsUnique();
            builder.HasIndex(x => x.ParticipantAUserId);
            builder.HasIndex(x => x.ParticipantBUserId);
        });

        modelBuilder.Entity<ConversationMessage>(builder =>
        {
            builder.ToTable("conversation_messages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Body).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasIndex(x => x.ConversationId);
        });
    }
}
