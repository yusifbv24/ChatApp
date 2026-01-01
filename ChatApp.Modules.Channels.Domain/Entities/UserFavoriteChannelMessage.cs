using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Channels.Domain.Entities
{
    public class UserFavoriteChannelMessage : Entity
    {
        public Guid UserId { get; private set; }
        public Guid MessageId { get; private set; }
        public DateTime FavoritedAtUtc { get; private set; }

        // Navigation property
        public ChannelMessage Message { get; private set; } = null!;

        private UserFavoriteChannelMessage() : base() { }

        public UserFavoriteChannelMessage(Guid userId, Guid messageId) : base()
        {
            UserId = userId;
            MessageId = messageId;
            FavoritedAtUtc = DateTime.UtcNow;
        }
    }
}