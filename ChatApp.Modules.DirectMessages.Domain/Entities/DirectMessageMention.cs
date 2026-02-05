using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.DirectMessages.Domain.Entities
{
    /// <summary>
    /// Represents a mention (@FullName) in a direct message.
    /// </summary>
    public class DirectMessageMention : Entity
    {
        public Guid MessageId { get; private set; }
        public Guid MentionedUserId { get; private set; }
        public string MentionedUserFullName { get; private set; } = string.Empty;

        // Navigation property
        public DirectMessage Message { get; private set; } = null!;

        private DirectMessageMention() { }

        public DirectMessageMention(Guid messageId, Guid mentionedUserId, string mentionedUserFullName)
        {
            if (mentionedUserId == Guid.Empty)
                throw new ArgumentException("Mentioned user ID cannot be empty", nameof(mentionedUserId));

            if (string.IsNullOrWhiteSpace(mentionedUserFullName))
                throw new ArgumentException("Mentioned user full name cannot be empty", nameof(mentionedUserFullName));

            if (mentionedUserFullName.Length > 255)
                throw new ArgumentException("Mentioned user full name cannot exceed 255 characters", nameof(mentionedUserFullName));

            MessageId = messageId;
            MentionedUserId = mentionedUserId;
            MentionedUserFullName = mentionedUserFullName;
        }
    }
}