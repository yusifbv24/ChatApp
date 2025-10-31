using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Events
{
    public class UserCreatedEvent:DomainEvent
    {
        public Guid UserId { get; }
        public string UserName { get; }
        public string DisplayName { get; }
        public Guid CreatedBy { get; }

        public UserCreatedEvent(Guid userId,string userName,string displayName,Guid createdBy)
        {
            UserId = userId;
            UserName = userName;
            DisplayName = displayName;
            CreatedBy = createdBy;
        }
    }
}