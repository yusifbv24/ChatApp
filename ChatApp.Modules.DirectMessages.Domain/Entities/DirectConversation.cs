using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.DirectMessages.Domain.Entities
{
    public class DirectConversation : Entity
    {
        public Guid User1Id { get; private set; }
        public Guid User2Id { get; private set; }
        public DateTime LastMessageAtUtc { get; private set; }

        /// <summary>
        /// The user who initiated/created this conversation.
        /// Used to hide empty conversations from the recipient until a message is sent.
        /// </summary>
        public Guid InitiatedByUserId { get; private set; }

        /// <summary>
        /// Indicates if any message has been sent in this conversation.
        /// Used to determine visibility for non-initiator.
        /// </summary>
        public bool HasMessages { get; private set; }

        /// <summary>
        /// Notes conversation (self-conversation) - User1Id = User2Id
        /// Always visible in conversation list
        /// </summary>
        public bool IsNotes { get; private set; }

        // Navigation properties
        public ICollection<DirectMessage> Messages { get; private set; } = [];
        public ICollection<DirectConversationMember> Members { get; private set; } = [];


        private DirectConversation() { }


        public DirectConversation(Guid user1Id, Guid user2Id, Guid initiatedByUserId, bool isNotes = false)
        {
            IsNotes = isNotes;

            // Notes conversation: User1Id = User2Id (self-conversation)
            if (isNotes)
            {
                User1Id = user1Id;
                User2Id = user1Id;
            }
            else
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
            }

            InitiatedByUserId = initiatedByUserId;
            HasMessages = isNotes; // Notes conversation always visible
            LastMessageAtUtc = DateTime.UtcNow;

            // Create member records for both users
            Members.Add(new DirectConversationMember(Id, User1Id));
            if (User1Id != User2Id) // Don't create duplicate member for Notes
            {
                Members.Add(new DirectConversationMember(Id, User2Id));
            }
        }

        /// <summary>
        /// Mark that the conversation has messages (called when first message is sent)
        /// </summary>
        public void MarkAsHasMessages()
        {
            HasMessages = true;
            UpdateTimestamp();
        }


        public void UpdateLastMessageTime()
        {
            LastMessageAtUtc = DateTime.UtcNow;
            UpdatedAtUtc=DateTime.UtcNow;
        }


        public bool IsParticipant(Guid userId)
        {
            return userId==User1Id || userId==User2Id;
        }


        public Guid GetOtherUserId(Guid currentUserId)
        {
            // Notes conversation: return self
            if (IsNotes)
                return currentUserId;

            if (currentUserId == User1Id)
                return User2Id;
            if (currentUserId == User2Id)
                return User1Id;

            throw new InvalidOperationException("User is not a participant in this conversation");
        }
    }
}