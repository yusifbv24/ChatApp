using ChatApp.Modules.DirectMessages.Domain.Entities;

namespace ChatApp.Modules.DirectMessages.Application.Interfaces
{
    public interface IDirectConversationMemberRepository
    {
        Task<DirectConversationMember?> GetByConversationAndUserAsync(
            Guid conversationId,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task AddAsync(DirectConversationMember member, CancellationToken cancellationToken = default);

        Task UpdateAsync(DirectConversationMember member, CancellationToken cancellationToken = default);
    }
}