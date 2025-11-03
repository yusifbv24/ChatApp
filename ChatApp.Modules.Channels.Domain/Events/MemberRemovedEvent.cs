using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Channels.Domain.Events
{
    public record MemberRemovedEvent : DomainEvent
    {
        public Guid ChannelId { get; }
        public Guid UserId { get; }
        public Guid RemovedBy { get; }

        public MemberRemovedEvent(Guid channelId, Guid userId, Guid removedBy)
        {
            ChannelId = channelId;
            UserId = userId;
            RemovedBy = removedBy;
        }
    }
}