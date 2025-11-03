using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Events
{
    public record UserDeletedEvent:DomainEvent
    {
        public Guid UserId { get; }
        public string UserName { get; }

        public UserDeletedEvent(Guid userId,string username)
        {
            UserId= userId;
            UserName= username;
        }
    }
}