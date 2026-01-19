using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.DirectMessages.Domain.Entities
{
    public class DirectConversationMember : Entity
    {
        public Guid ConversationId { get; private set; }
        public Guid UserId { get; private set; }
        public bool IsActive { get; private set; }

        // Read Later support
        public Guid? LastReadLaterMessageId { get; private set; }

        // Conversation-level preferences
        public bool IsPinned { get; private set; }
        public bool IsMuted { get; private set; }
        public bool IsMarkedReadLater { get; private set; }

        // Navigation properties
        public DirectConversation Conversation { get; private set; } = null!;

        private DirectConversationMember() { }

        public DirectConversationMember(Guid conversationId, Guid userId)
        {
            ConversationId = conversationId;
            UserId = userId;
            IsActive = true;
        }

        public void MarkAsInactive()
        {
            IsActive = false;
            UpdateTimestamp();
        }

        public void MarkAsActive()
        {
            IsActive = true;
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

        public void TogglePin()
        {
            IsPinned = !IsPinned;
            UpdateTimestamp();
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            UpdateTimestamp();
        }

        public void MarkConversationAsReadLater()
        {
            IsMarkedReadLater = true;
            UpdateTimestamp();
        }

        public void UnmarkConversationAsReadLater()
        {
            IsMarkedReadLater = false;
            UpdateTimestamp();
        }
    }
}