using ChatApp.Modules.Channels.Domain.Entities;

namespace ChatApp.Modules.Channels.Application.Interfaces
{
    public interface IUserFavoriteChannelMessageRepository
    {
        Task<UserFavoriteChannelMessage?> GetAsync(Guid userId, Guid messageId, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetFavoriteMessageIdsAsync(Guid userId, Guid channelId, CancellationToken cancellationToken = default);
        Task<bool> IsFavoriteAsync(Guid userId, Guid messageId, CancellationToken cancellationToken = default);
        Task AddAsync(UserFavoriteChannelMessage favorite, CancellationToken cancellationToken = default);
        Task RemoveAsync(UserFavoriteChannelMessage favorite, CancellationToken cancellationToken = default);
    }
}
