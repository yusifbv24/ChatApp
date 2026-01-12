using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.DirectMessages.Domain.Entities
{
    /// <summary>
    /// Represents a mention (@UserName) in a direct message.
    /// </summary>
    public class DirectMessageMention : Entity
    {
        public Guid MessageId { get; private set; }
        public Guid MentionedUserId { get; private set; }
        public string MentionedUserName { get; private set; } = string.Empty;

        // Navigation property
        public DirectMessage Message { get; private set; } = null!;

        private DirectMessageMention() { }

        public DirectMessageMention(Guid messageId, Guid mentionedUserId, string mentionedUserName)
        {
            if (mentionedUserId == Guid.Empty)
                throw new ArgumentException("Mentioned user ID cannot be empty", nameof(mentionedUserId));

            if (string.IsNullOrWhiteSpace(mentionedUserName))
                throw new ArgumentException("Mentioned user name cannot be empty", nameof(mentionedUserName));

            if (mentionedUserName.Length > 255)
                throw new ArgumentException("Mentioned user name cannot exceed 255 characters", nameof(mentionedUserName));

            MessageId = messageId;
            MentionedUserId = mentionedUserId;
            MentionedUserName = mentionedUserName;
        }
    }
}