using ChatApp.Modules.DirectMessages.Domain.Entities;

namespace ChatApp.Modules.DirectMessages.Application.Interfaces
{
    public interface IUserFavoriteMessageRepository
    {
        Task<UserFavoriteMessage?> GetAsync(Guid userId, Guid messageId, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetFavoriteMessageIdsAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default);
        Task<bool> IsFavoriteAsync(Guid userId, Guid messageId, CancellationToken cancellationToken = default);
        Task AddAsync(UserFavoriteMessage favorite, CancellationToken cancellationToken = default);
        Task RemoveAsync(UserFavoriteMessage favorite, CancellationToken cancellationToken = default);
    }
}
