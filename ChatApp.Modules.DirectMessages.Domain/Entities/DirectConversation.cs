using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.DirectMessages.Domain.Entities
{
    public class DirectConversation : Entity
    {
        public Guid User1Id { get; private set; }
        public Guid User2Id { get; private set; }
        public DateTime LastMessageAtUtc { get; private set; }
        public bool IsUser1Active { get; private set; }
        public bool IsUser2Active { get; private set; }


        // Navigation properties
        public ICollection<DirectMessage> Messages { get; private set; } = [];


        private DirectConversation() { }


        public DirectConversation(Guid user1Id,Guid user2Id)
        {
            // Always store users in consistent order (smaller GUID first) for easy lookups
            if (user1Id < user2Id)
            {
                User1Id = user1Id;
                User2Id = user2Id;
            }
            else
            {
                User1Id = user2Id;
                User2Id = user1Id;
            }

            LastMessageAtUtc=DateTime.UtcNow;
            IsUser1Active = true;
            IsUser2Active = true;
        }


        public void UpdateLastMessageTime()
        {
            LastMessageAtUtc = DateTime.UtcNow;
            UpdatedAtUtc=DateTime.UtcNow;
        }

        public void MarkUserAsInactive(Guid userId)
        {
            if(userId==User1Id)
                IsUser1Active=false;
            else if(userId==User2Id)
                IsUser2Active=false;

            UpdatedAtUtc = DateTime.UtcNow;
        }


        public void MarkUserAsActive(Guid userId)
        {
            if(userId == User1Id)
                IsUser1Active=true;

            else if(userId == User2Id)
                IsUser2Active=false;

            UpdatedAtUtc=DateTime.UtcNow;
        }


        public bool IsParticipant(Guid userId)
        {
            return userId==User1Id || userId==User2Id;
        }


        public Guid GetOtherUserId(Guid currentUserId)
        {
            if (currentUserId == User1Id)
                return User2Id;
            if (currentUserId == User2Id)
                return User1Id;

            throw new InvalidOperationException("User is not a participant in this conversation");
        }
    }
}