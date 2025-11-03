using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Channels.Domain.Events
{
    public record ChannelCreatedEvent:DomainEvent
    {
        public Guid ChannelId { get; set; }
        public string ChannelName { get; set; }
        public Guid CreatedBy {  get; set; }

        public ChannelCreatedEvent(Guid channelId,string channelName,Guid createdBy)
        {
            ChannelId= channelId;
            ChannelName= channelName;
            CreatedBy= createdBy;
        }
    }
}