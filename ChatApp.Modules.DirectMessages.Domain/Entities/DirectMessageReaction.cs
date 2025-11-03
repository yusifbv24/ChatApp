using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.DirectMessages.Domain.Entities
{
    public class DirectMessageReaction : Entity
    {
        public Guid MessageId { get; private set;  }
        public Guid UserId { get; private set; }
        public string Reaction { get; private set; } = string.Empty;

        // Navigation property
        public DirectMessage Message { get; private set; } = null!;


        private DirectMessageReaction() { }

        public DirectMessageReaction(Guid messageId,Guid userId, string reaction)
        {
            if (string.IsNullOrWhiteSpace(reaction))
                throw new ArgumentException("Reaction cannot be empty");

            if (reaction.Length > 10)
                throw new ArgumentException("Reaction must be a single emoji");

            MessageId= messageId;
            UserId= userId;
            Reaction= reaction;
        }
    }
}