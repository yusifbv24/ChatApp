using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Entities;
using ChatApp.Modules.Channels.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ChatApp.Modules.Channels.Infrastructure.Persistence.Repositories
{
    public class ChannelRepository : IChannelRepository
    {
        private readonly ChannelsDbContext _context;

        public ChannelRepository(ChannelsDbContext context)
        {
            _context = context;
        }

        public async Task<Channel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Channels
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<Channel?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Channels
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<ChannelDetailsDto?> GetChannelDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // Get channel with creator username
            var channelWithCreator = await (from channel in _context.Channels
                                            join creator in _context.Set<UserReadModel>() on channel.CreatedBy equals creator.Id
                                            where channel.Id == id
                                            select new
                                            {
                                                channel.Id,
                                                channel.Name,
                                                channel.Description,
                                                channel.Type,
                                                channel.CreatedBy,
                                                CreatorUsername = creator.Username,
                                                channel.IsArchived,
                                                channel.CreatedAtUtc
                                            })
                                           .FirstOrDefaultAsync(cancellationToken);

            if (channelWithCreator == null)
                return null;

            // Get members with user details
            var members = await (from member in _context.ChannelMembers
                                 join user in _context.Set<UserReadModel>() on member.UserId equals user.Id
                                 where member.ChannelId == id && member.IsActive
                                 orderby member.Role descending, member.JoinedAtUtc
                                 select new ChannelMemberDto(
                                     member.Id,
                                     member.ChannelId,
                                     member.UserId,
                                     user.Username,
                                     user.DisplayName,
                                     member.Role,
                                     member.JoinedAtUtc,
                                     member.IsActive
                                 ))
                                .ToListAsync(cancellationToken);

            return new ChannelDetailsDto(
                channelWithCreator.Id,
                channelWithCreator.Name,
                channelWithCreator.Description,
                channelWithCreator.Type,
                channelWithCreator.CreatedBy,
                channelWithCreator.CreatorUsername,
                channelWithCreator.IsArchived,
                members.Count,
                members,
                channelWithCreator.CreatedAtUtc
            );
        }

        public async Task<Channel?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Channels
                .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
        }

        public async Task<List<Channel>> GetUserChannelsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Channels
                .Include(c => c.Members)
                .Where(c => c.Members.Any(m => m.UserId == userId && m.IsActive))
                .Where(c => !c.IsArchived)
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ChannelDto>> GetUserChannelDtosAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // Get channels where user is a member with last message info
            var channelsWithLastMessage = await (
                from channel in _context.Channels
                join member in _context.ChannelMembers on channel.Id equals member.ChannelId
                where member.UserId == userId && member.IsActive && !channel.IsArchived
                // Get last message for each channel (LEFT JOIN)
                let lastMessage = (from msg in _context.ChannelMessages
                                   join sender in _context.Set<UserReadModel>() on msg.SenderId equals sender.Id
                                   where msg.ChannelId == channel.Id && !msg.IsDeleted
                                   orderby msg.CreatedAtUtc descending
                                   select new
                                   {
                                       msg.Content,
                                       sender.DisplayName,
                                       msg.CreatedAtUtc
                                   }).FirstOrDefault()
                // Get member count
                let memberCount = _context.ChannelMembers.Count(m => m.ChannelId == channel.Id && m.IsActive)
                // Get unread count (messages after user's last read)
                let lastReadTime = (from read in _context.ChannelMessageReads
                                    join msg in _context.ChannelMessages on read.MessageId equals msg.Id
                                    where read.UserId == userId && msg.ChannelId == channel.Id
                                    orderby read.ReadAtUtc descending
                                    select (DateTime?)read.ReadAtUtc).FirstOrDefault()
                let unreadCount = _context.ChannelMessages.Count(m =>
                    m.ChannelId == channel.Id &&
                    !m.IsDeleted &&
                    m.SenderId != userId &&
                    (lastReadTime == null || m.CreatedAtUtc > lastReadTime.Value))
                select new ChannelDto(
                    channel.Id,
                    channel.Name,
                    channel.Description,
                    channel.Type,
                    channel.CreatedBy,
                    memberCount,
                    channel.IsArchived,
                    channel.CreatedAtUtc,
                    channel.ArchivedAtUtc,
                    lastMessage != null ? lastMessage.Content : null,
                    lastMessage != null ? lastMessage.DisplayName : null,
                    lastMessage != null ? lastMessage.CreatedAtUtc : (DateTime?)null,
                    unreadCount
                )
            ).ToListAsync(cancellationToken);

            return channelsWithLastMessage
                .OrderByDescending(c => c.LastMessageAtUtc ?? c.CreatedAtUtc)
                .ToList();
        }

        public async Task<List<Channel>> GetPublicChannelsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Channels
                .Include(c => c.Members)
                .Where(c => c.Type == ChannelType.Public)
                .Where(c => !c.IsArchived)
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsUserMemberAsync(Guid channelId, Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.ChannelMembers
                .AnyAsync(m => m.ChannelId == channelId && m.UserId == userId && m.IsActive, cancellationToken);
        }

        public async Task<bool> ExistsAsync(Expression<Func<Channel, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Channels.AnyAsync(predicate, cancellationToken);
        }

        public async Task AddAsync(Channel channel, CancellationToken cancellationToken = default)
        {
            await _context.Channels.AddAsync(channel, cancellationToken);
        }

        public Task UpdateAsync(Channel channel, CancellationToken cancellationToken = default)
        {
            _context.Channels.Update(channel);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Channel channel, CancellationToken cancellationToken = default)
        {
            _context.Channels.Remove(channel);
            return Task.CompletedTask;
        }
    }
}