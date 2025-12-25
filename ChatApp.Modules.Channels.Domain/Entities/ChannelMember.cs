using ChatApp.Modules.Channels.Domain.Enums;
using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Channels.Domain.Entities
{
    public class ChannelMember : Entity
    {
        public Guid ChannelId { get; private set; }
        public Guid UserId { get; private set; }
        public MemberRole Role { get; private set; }
        public DateTime JoinedAtUtc { get; private set; }
        public DateTime? LeftAtUtc { get; private set; }
        public bool IsActive { get; private set; }
        public Guid? LastReadLaterMessageId { get; private set; }

        // Navigation properties
        public Channel Channel { get; private set; } = null!;

        private ChannelMember() : base() { }

        public ChannelMember(Guid channelId, Guid userId, MemberRole role) : base()
        {
            ChannelId = channelId;
            UserId = userId;
            Role = role;
            JoinedAtUtc = DateTime.UtcNow;
            IsActive = true;
        }

        public void UpdateRole(MemberRole newRole)
        {
            Role = newRole;
            UpdateTimestamp();
        }

        public void Leave()
        {
            IsActive = false;
            LeftAtUtc = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void Rejoin()
        {
            IsActive = true;
            LeftAtUtc = null;
            JoinedAtUtc = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void MarkMessageAsLater(Guid messageId)
        {
            LastReadLaterMessageId = messageId;
            UpdateTimestamp();
        }

        public void UnmarkMessageAsLater()
        {
            LastReadLaterMessageId = null;
            UpdateTimestamp();
        }
    }
}