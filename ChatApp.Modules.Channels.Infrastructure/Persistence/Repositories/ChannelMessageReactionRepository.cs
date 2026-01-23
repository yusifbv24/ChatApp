using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Channels.Infrastructure.Persistence.Repositories
{
    public class ChannelMessageReactionRepository : IChannelMessageReactionRepository
    {
        private readonly ChannelsDbContext _context;

        public ChannelMessageReactionRepository(ChannelsDbContext context)
        {
            _context = context;
        }

        public async Task<ChannelMessageReaction?> GetReactionAsync(
            Guid messageId,
            Guid userId,
            string reaction,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<ChannelMessageReaction>()
                .FirstOrDefaultAsync(r =>
                    r.MessageId == messageId &&
                    r.UserId == userId &&
                    r.Reaction == reaction,
                    cancellationToken);
        }

        public async Task<List<ChannelMessageReaction>> GetMessageReactionsAsync(
            Guid messageId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<ChannelMessageReaction>()
                .Where(r => r.MessageId == messageId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ChannelMessageReactionDto>> GetMessageReactionsWithUserDetailsAsync(
            Guid messageId,
            CancellationToken cancellationToken = default)
        {
            return await (
                from reaction in _context.Set<ChannelMessageReaction>()
                join user in _context.Set<UserReadModel>() on reaction.UserId equals user.Id
                where reaction.MessageId == messageId
                group new { reaction, user } by reaction.Reaction into g
                select new ChannelMessageReactionDto(
                    g.Key,
                    g.Count(),
                    g.Select(x => x.reaction.UserId).ToList(),
                    g.Select(x => x.user.FullName).ToList(),
                    g.Select(x => x.user.AvatarUrl).ToList()
                ))
                .ToListAsync(cancellationToken);
        }

        public async Task AddReactionAsync(
            ChannelMessageReaction reaction,
            CancellationToken cancellationToken = default)
        {
            await _context.Set<ChannelMessageReaction>().AddAsync(reaction, cancellationToken);
        }

        public Task RemoveReactionAsync(
            ChannelMessageReaction reaction,
            CancellationToken cancellationToken = default)
        {
            _context.Set<ChannelMessageReaction>().Remove(reaction);
            return Task.CompletedTask;
        }
    }
}
