using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Events
{
    public record UserPasswordChangedEvent:DomainEvent
    {
        public Guid UserId { get; }
        public UserPasswordChangedEvent(Guid userId)
        {
            UserId = userId;
        }
    }
}