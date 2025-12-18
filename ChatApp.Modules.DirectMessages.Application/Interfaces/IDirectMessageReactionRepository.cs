using ChatApp.Modules.DirectMessages.Application.Commands.DirectMessageReactions;
using ChatApp.Modules.DirectMessages.Domain.Entities;

namespace ChatApp.Modules.DirectMessages.Application.Interfaces
{
    public interface IDirectMessageReactionRepository
    {
        Task AddAsync(DirectMessageReaction reaction, CancellationToken cancellationToken = default);
        Task DeleteAsync(DirectMessageReaction reaction, CancellationToken cancellationToken = default);
        Task<List<ReactionSummary>> GetMessageReactionsWithUserDetailsAsync(Guid messageId, CancellationToken cancellationToken = default);
    }
}
