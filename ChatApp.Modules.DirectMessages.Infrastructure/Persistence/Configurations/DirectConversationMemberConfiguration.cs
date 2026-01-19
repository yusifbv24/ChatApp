using ChatApp.Modules.DirectMessages.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Modules.DirectMessages.Infrastructure.Persistence.Configurations
{
    public class DirectConversationMemberConfiguration : IEntityTypeConfiguration<DirectConversationMember>
    {
        public void Configure(EntityTypeBuilder<DirectConversationMember> builder)
        {
            builder.ToTable("direct_conversation_members");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.ConversationId)
                .IsRequired();

            builder.Property(m => m.UserId)
                .IsRequired();

            builder.Property(m => m.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(m => m.IsPinned)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(m => m.IsMuted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(m => m.IsMarkedReadLater)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(m => m.LastReadLaterMessageId)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(m => new { m.ConversationId, m.UserId })
                .IsUnique();

            builder.HasIndex(m => m.UserId);

            builder.HasIndex(m => new { m.UserId, m.IsActive });

            // Relationship
            builder.HasOne(m => m.Conversation)
                .WithMany(c => c.Members)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Timestamps
            builder.Property(m => m.CreatedAtUtc)
                .IsRequired();

            builder.Property(m => m.UpdatedAtUtc)
                .IsRequired();
        }
    }
}
