using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Domain.Entities;
using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.DirectMessages.Application.Interfaces
{
    public interface IDirectConversationRepository
    {
        Task<DirectConversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<DirectConversation?> GetByParticipantsAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default);
        Task<PagedResult<DirectConversationDto>> GetUserConversationsPagedAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default);
        Task AddAsync(DirectConversation conversation, CancellationToken cancellationToken = default);
        Task UpdateAsync(DirectConversation conversation, CancellationToken cancellationToken = default);
        Task DeleteAsync(DirectConversation conversation, CancellationToken cancellationToken = default);
    }
}