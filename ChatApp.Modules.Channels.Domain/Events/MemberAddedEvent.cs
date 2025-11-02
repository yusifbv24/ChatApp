using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Channels.Domain.Events
{
    public class MemberAddedEvent : DomainEvent
    {
        public Guid ChannelId { get; }
        public Guid UserId { get; }
        public Guid AddedBy { get; }

        public MemberAddedEvent(Guid channelId, Guid userId, Guid addedBy)
        {
            ChannelId = channelId;
            UserId = userId;
            AddedBy = addedBy;
        }
    }
}