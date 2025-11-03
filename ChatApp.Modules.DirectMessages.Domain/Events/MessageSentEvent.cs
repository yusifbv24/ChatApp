using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.DirectMessages.Domain.Events
{
    public record MessageSentEvent(
        Guid MessageId,
        Guid ConversationId,
        Guid SenderId,
        Guid ReceiverId,
        string Content,
        DateTime SentAtUtc
    ):DomainEvent;
}