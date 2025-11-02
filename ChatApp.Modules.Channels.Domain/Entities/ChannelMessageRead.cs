using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Channels.Domain.Entities
{
    public class ChannelMessageRead : Entity
    {
        public Guid MessageId { get; private set; }
        public Guid UserId { get; private set; }
        public DateTime ReadAtUtc { get; private set; }

        // Navigation property
        public ChannelMessage Message { get; private set; } = null!;

        private ChannelMessageRead() : base() { }

        public ChannelMessageRead(Guid messageId, Guid userId) : base()
        {
            MessageId = messageId;
            UserId = userId;
            ReadAtUtc = DateTime.UtcNow;
        }
    }
}