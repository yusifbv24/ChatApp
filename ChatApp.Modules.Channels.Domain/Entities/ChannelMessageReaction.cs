using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Channels.Domain.Entities
{
    public class ChannelMessageReaction : Entity
    {
        public Guid MessageId { get; private set; }
        public Guid UserId { get; private set; }
        public string Reaction { get; private set; } = null!; // emoji like "👍"

        // Navigation property
        public ChannelMessage Message { get; private set; } = null!;

        private ChannelMessageReaction() : base() { }

        public ChannelMessageReaction(Guid messageId, Guid userId, string reaction) : base()
        {
            if (string.IsNullOrWhiteSpace(reaction))
                throw new ArgumentException("Reaction cannot be empty", nameof(reaction));

            if (reaction.Length > 10)
                throw new ArgumentException("Reaction must be a single emoji", nameof(reaction));

            MessageId = messageId;
            UserId = userId;
            Reaction = reaction;
        }
    }
}