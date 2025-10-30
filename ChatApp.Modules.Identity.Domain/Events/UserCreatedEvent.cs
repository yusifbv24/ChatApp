using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Events
{
    public class UserCreatedEvent:DomainEvent
    {
        public Guid UserId { get; }
        public string UserName { get; }
        public string Email { get; }

        public UserCreatedEvent(Guid userId,string userName,string email)
        {
            UserId = userId;
            UserName = userName;
            Email = email;
        }
    }
}