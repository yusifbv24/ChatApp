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

        // Read Later support (per user)
        public Guid? User1LastReadLaterMessageId { get; private set; }
        public Guid? User2LastReadLaterMessageId { get; private set; }

        // Conversation-level preferences (per user)
        public bool User1IsPinned { get; private set; }
        public bool User2IsPinned { get; private set; }
        public bool User1IsMuted { get; private set; }
        public bool User2IsMuted { get; private set; }
        public bool User1IsMarkedReadLater { get; private set; }
        public bool User2IsMarkedReadLater { get; private set; }

        /// <summary>
        /// Notes conversation (self-conversation) - User1Id = User2Id
        /// Always visible in conversation list
        /// </summary>
        public bool IsNotes { get; private set; }

        // Navigation properties
        public ICollection<DirectMessage> Messages { get; private set; } = [];


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
            IsUser1Active = true;
            IsUser2Active = true;
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
            // Notes conversation: return self
            if (IsNotes)
                return currentUserId;

            if (currentUserId == User1Id)
                return User2Id;
            if (currentUserId == User2Id)
                return User1Id;

            throw new InvalidOperationException("User is not a participant in this conversation");
        }

        public void MarkMessageAsLater(Guid userId, Guid messageId)
        {
            if (userId == User1Id)
            {
                User1LastReadLaterMessageId = messageId;
            }
            else if (userId == User2Id)
            {
                User2LastReadLaterMessageId = messageId;
            }
            else
            {
                throw new InvalidOperationException("User is not a participant in this conversation");
            }
            UpdateTimestamp();
        }

        public void UnmarkMessageAsLater(Guid userId)
        {
            if (userId == User1Id)
            {
                User1LastReadLaterMessageId = null;
            }
            else if (userId == User2Id)
            {
                User2LastReadLaterMessageId = null;
            }
            else
            {
                throw new InvalidOperationException("User is not a participant in this conversation");
            }
            UpdateTimestamp();
        }

        public Guid? GetLastReadLaterMessageId(Guid userId)
        {
            if (userId == User1Id)
                return User1LastReadLaterMessageId;
            if (userId == User2Id)
                return User2LastReadLaterMessageId;

            throw new InvalidOperationException("User is not a participant in this conversation");
        }

        public void TogglePin(Guid userId)
        {
            if (userId == User1Id)
            {
                User1IsPinned = !User1IsPinned;
            }
            else if (userId == User2Id)
            {
                User2IsPinned = !User2IsPinned;
            }
            else
            {
                throw new InvalidOperationException("User is not a participant in this conversation");
            }
            UpdateTimestamp();
        }

        public void ToggleMute(Guid userId)
        {
            if (userId == User1Id)
            {
                User1IsMuted = !User1IsMuted;
            }
            else if (userId == User2Id)
            {
                User2IsMuted = !User2IsMuted;
            }
            else
            {
                throw new InvalidOperationException("User is not a participant in this conversation");
            }
            UpdateTimestamp();
        }

        public void MarkConversationAsReadLater(Guid userId)
        {
            if (userId == User1Id)
            {
                User1IsMarkedReadLater = true;
            }
            else if (userId == User2Id)
            {
                User2IsMarkedReadLater = true;
            }
            else
            {
                throw new InvalidOperationException("User is not a participant in this conversation");
            }
            UpdateTimestamp();
        }

        public void UnmarkConversationAsReadLater(Guid userId)
        {
            if (userId == User1Id)
            {
                User1IsMarkedReadLater = false;
            }
            else if (userId == User2Id)
            {
                User2IsMarkedReadLater = false;
            }
            else
            {
                throw new InvalidOperationException("User is not a participant in this conversation");
            }
            UpdateTimestamp();
        }

        public bool IsPinned(Guid userId)
        {
            if (userId == User1Id)
                return User1IsPinned;
            if (userId == User2Id)
                return User2IsPinned;

            throw new InvalidOperationException("User is not a participant in this conversation");
        }

        public bool IsMuted(Guid userId)
        {
            if (userId == User1Id)
                return User1IsMuted;
            if (userId == User2Id)
                return User2IsMuted;

            throw new InvalidOperationException("User is not a participant in this conversation");
        }

        public bool IsMarkedReadLater(Guid userId)
        {
            if (userId == User1Id)
                return User1IsMarkedReadLater;
            if (userId == User2Id)
                return User2IsMarkedReadLater;

            throw new InvalidOperationException("User is not a participant in this conversation");
        }
    }
}