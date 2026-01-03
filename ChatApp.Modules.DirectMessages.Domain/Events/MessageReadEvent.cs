using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.DirectMessages.Domain.Events
{
    public record MessageReadEvent(
        Guid MessageId,
        Guid ConversationId,
        Guid ReadBy
    ): DomainEvent;
}