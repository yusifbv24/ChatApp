using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Events
{
    public record UserDeletedEvent : DomainEvent
    {
        public Guid UserId { get; }
        public string Email { get; }

        public UserDeletedEvent(Guid userId, string email)
        {
            UserId = userId;
            Email = email;
        }
    }
}