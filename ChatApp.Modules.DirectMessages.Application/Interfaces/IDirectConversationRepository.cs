using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Domain.Entities;

namespace ChatApp.Modules.DirectMessages.Application.Interfaces
{
    public interface IDirectConversationRepository
    {
        Task<DirectConversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<DirectConversation?> GetByParticipantsAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default);
        Task<List<DirectConversationDto>> GetUserConversationsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default);
        Task AddAsync(DirectConversation conversation, CancellationToken cancellationToken = default);
        Task UpdateAsync(DirectConversation conversation, CancellationToken cancellationToken = default);
        Task DeleteAsync(DirectConversation conversation, CancellationToken cancellationToken = default);
    }
}