using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Channels.Domain.Events
{
    public record ChannelMessageSentEvent : DomainEvent
    {
        public Guid MessageId { get; }
        public Guid ChannelId { get; }
        public Guid SenderId { get; }
        public string Content { get; }

        public ChannelMessageSentEvent(Guid messageId, Guid channelId, Guid senderId, string content)
        {
            MessageId = messageId;
            ChannelId = channelId;
            SenderId = senderId;
            Content = content;
        }
    }
}