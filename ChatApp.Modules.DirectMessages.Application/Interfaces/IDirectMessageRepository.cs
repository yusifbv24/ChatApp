using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Domain.Entities;

namespace ChatApp.Modules.DirectMessages.Application.Interfaces
{
    public interface IDirectMessageRepository
    {
        Task<DirectMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<DirectMessage?> GetByIdWithReactionsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<DirectMessageDto?> GetByIdAsDtoAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<DirectMessageDto>> GetConversationMessagesAsync(
            Guid conversationId,
            int pageSize = 50,
            DateTime? beforeUtc = null,
            CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
        Task AddAsync(DirectMessage message, CancellationToken cancellationToken = default);
        Task DeleteAsync(DirectMessage message, CancellationToken cancellationToken = default);
    }
}