using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Channels.Domain.Entities
{
    /// <summary>
    /// Represents a mention (@FullName or @All) in a channel message.
    /// </summary>
    public class ChannelMessageMention : Entity
    {
        public Guid MessageId { get; private set; }
        public Guid? MentionedUserId { get; private set; } // Null for @All
        public string MentionedUserFullName { get; private set; } = string.Empty;
        public bool IsAllMention { get; private set; }

        // Navigation property
        public ChannelMessage Message { get; private set; } = null!;

        private ChannelMessageMention() : base() { }

        public ChannelMessageMention(Guid messageId, Guid? mentionedUserId, string mentionedUserFullName, bool isAllMention = false) : base()
        {
            if (string.IsNullOrWhiteSpace(mentionedUserFullName))
                throw new ArgumentException("Mentioned user full name cannot be empty", nameof(mentionedUserFullName));

            if (mentionedUserFullName.Length > 255)
                throw new ArgumentException("Mentioned user full name cannot exceed 255 characters", nameof(mentionedUserFullName));

            // Validation: @All should have null userId and isAllMention=true
            if (isAllMention && mentionedUserId.HasValue)
                throw new ArgumentException("@All mention should not have a specific user ID");

            if (!isAllMention && !mentionedUserId.HasValue)
                throw new ArgumentException("User mention must have a valid user ID");

            MessageId = messageId;
            MentionedUserId = mentionedUserId;
            MentionedUserFullName = mentionedUserFullName;
            IsAllMention = isAllMention;
        }
    }
}