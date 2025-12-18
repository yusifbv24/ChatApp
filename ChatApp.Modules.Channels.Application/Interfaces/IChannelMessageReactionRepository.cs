using ChatApp.Modules.Channels.Domain.Entities;

namespace ChatApp.Modules.Channels.Application.Interfaces
{
    public interface IChannelMessageReactionRepository
    {
        Task<ChannelMessageReaction?> GetReactionAsync(Guid messageId, Guid userId, string reaction, CancellationToken cancellationToken = default);
        Task<List<ChannelMessageReaction>> GetMessageReactionsAsync(Guid messageId, CancellationToken cancellationToken = default);
        Task AddReactionAsync(ChannelMessageReaction reaction, CancellationToken cancellationToken = default);
        Task RemoveReactionAsync(ChannelMessageReaction reaction, CancellationToken cancellationToken = default);
    }
}