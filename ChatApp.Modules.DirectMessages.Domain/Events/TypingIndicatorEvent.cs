using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.DirectMessages.Domain.Events
{
    public record TypingIndicatorEvent(
        Guid ConversationId,
        Guid UserId,
        bool IsTyping
    ): DomainEvent;
}