using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.DirectMessages.Domain.Entities
{
    public class UserFavoriteMessage : Entity
    {
        public Guid UserId { get; private set; }
        public Guid MessageId { get; private set; }
        public DateTime FavoritedAtUtc { get; private set; }

        // Navigation property
        public DirectMessage Message { get; private set; } = null!;

        private UserFavoriteMessage() { }

        public UserFavoriteMessage(Guid userId, Guid messageId)
        {
            UserId = userId;
            MessageId = messageId;
            FavoritedAtUtc = DateTime.UtcNow;
        }
    }
}
