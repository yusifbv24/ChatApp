using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Channels.Domain.Entities
{
    public class ChannelMessage : Entity
    {
        public Guid ChannelId { get; private set; }
        public Guid SenderId { get; private set; }
        public string Content { get; private set; } = null!;
        public string? FileId { get; private set; } // Reference to file service
        public bool IsEdited { get; private set; }
        public bool IsDeleted { get; private set; }
        public bool IsPinned { get; private set; }
        public DateTime? EditedAtUtc { get; private set; }
        public DateTime? DeletedAtUtc { get; private set; }
        public DateTime? PinnedAtUtc { get; private set; }
        public Guid? PinnedBy { get; private set; }

        // Navigation properties
        public Channel Channel { get; private set; } = null!;

        private readonly List<ChannelMessageReaction> _reactions = new();
        public IReadOnlyCollection<ChannelMessageReaction> Reactions => _reactions.AsReadOnly();

        private readonly List<ChannelMessageRead> _reads = new();
        public IReadOnlyCollection<ChannelMessageRead> Reads => _reads.AsReadOnly();

        private ChannelMessage() : base() { }

        public ChannelMessage(
            Guid channelId,
            Guid senderId,
            string content,
            string? fileId = null) : base()
        {
            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentException("Message must have content or file attachment");

            if (content?.Length > 4000)
                throw new ArgumentException("Message content cannot exceed 4000 characters");

            ChannelId = channelId;
            SenderId = senderId;
            Content = content ?? string.Empty;
            FileId = fileId;
            IsEdited = false;
            IsDeleted = false;
            IsPinned = false;
        }

        public void Edit(string newContent)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot edit deleted message");

            if (string.IsNullOrWhiteSpace(newContent))
                throw new ArgumentException("Message content cannot be empty", nameof(newContent));

            if (newContent.Length > 4000)
                throw new ArgumentException("Message content cannot exceed 4000 characters");

            Content = newContent;
            IsEdited = true;
            EditedAtUtc = DateTime.UtcNow;
            UpdateTimestamp();
        }

        // message content will never change. just marked as deleted and will not show to the user
        public void Delete()
        {
            IsDeleted = true;
            DeletedAtUtc = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void Pin(Guid pinnedBy)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot pin deleted message");

            IsPinned = true;
            PinnedAtUtc = DateTime.UtcNow;
            PinnedBy = pinnedBy;
            UpdateTimestamp();
        }

        public void Unpin()
        {
            IsPinned = false;
            PinnedAtUtc = null;
            PinnedBy = null;
            UpdateTimestamp();
        }

        public void AddReaction(ChannelMessageReaction reaction)
        {
            if (_reactions.Any(r => r.UserId == reaction.UserId && r.Reaction == reaction.Reaction))
                throw new InvalidOperationException("User has already reacted with this emoji");

            _reactions.Add(reaction);
            UpdateTimestamp();
        }

        public void RemoveReaction(Guid userId, string reactionEmoji)
        {
            var reaction = _reactions.FirstOrDefault(r => r.UserId == userId && r.Reaction == reactionEmoji);
            if (reaction != null)
            {
                _reactions.Remove(reaction);
                UpdateTimestamp();
            }
        }
    }
}