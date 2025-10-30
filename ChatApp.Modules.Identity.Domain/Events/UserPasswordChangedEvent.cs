using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Events
{
    public class UserPasswordChangedEvent:DomainEvent
    {
        public Guid UserId { get; }
        public UserPasswordChangedEvent(Guid userId)
        {
            UserId = userId;
        }
    }
}