using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Events
{
    public record UserCreatedEvent : DomainEvent
    {
        public Guid UserId { get; }
        public string FullName { get; }
        public string Email { get; }
        public Guid CreatedBy { get; }

        public UserCreatedEvent(Guid userId, string fullName, string email, Guid createdBy)
        {
            UserId = userId;
            FullName = fullName;
            Email = email;
            CreatedBy = createdBy;
        }
    }
}