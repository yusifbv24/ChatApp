using ChatApp.Modules.Search.Application.DTOs.Requests;
using ChatApp.Modules.Search.Application.DTOs.Responses;
using ChatApp.Modules.Search.Application.Interfaces;
using ChatApp.Modules.Search.Domain.Enums;
using ChatApp.Modules.Search.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Search.Infrastructure.Repositories
{
    public class SearchRepository : ISearchRepository
    {
        private readonly SearchDbContext _context;
        private readonly ILogger<SearchRepository> _logger;

        public SearchRepository(
            SearchDbContext context,
            ILogger<SearchRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SearchResultsDto> SearchChannelMessagesAsync(
            Guid userId, 
            string searchTerm, 
            Guid? specificChannelId,
            int pageNumber, 
            int pageSize, 
            CancellationToken cancellationToken = default)
        {
            var query = from message in _context.Set<ChannelMessageReadModel>()
                        join channel in _context.Set<ChannelReadModel>()
                            on message.ChannelId equals channel.Id
                        join sender in _context.Set<UserReadModel>()
                            on message.SenderId equals sender.Id
                        join member in _context.Set<ChannelMemberReadModel>()
                            on new { message.ChannelId, UserId = userId }
                            equals new { member.ChannelId, member.UserId }
                        where !message.IsDeleted
                              && member.LeftAtUtc == null // User is still a member
                              && EF.Functions.ILike(message.Content, $"%{searchTerm}%") // Case-insensitive search
                        select new SearchResultDto(
                            message.Id,
                            SearchResultType.ChannelMessage,
                            message.Content,
                            HighlightSearchTerm(message.Content, searchTerm),
                            sender.Id,
                            sender.Username,
                            sender.DisplayName,
                            sender.AvatarUrl,
                            message.CreatedAtUtc,
                            message.ChannelId,
                            channel.Name,
                            null,
                            null,
                            null,
                            null
                        );

            // Filter by specific channel if provided
            if (specificChannelId.HasValue)
            {
                query=query.Where(r=>r.ChannelId== specificChannelId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var results = await query
                .OrderByDescending(r => r.CreatedAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var hasNextPage = totalCount > (pageNumber * pageSize);

            return new SearchResultsDto(
                results,
                totalCount,
                pageNumber,
                pageSize,
                hasNextPage);
        }


        public async Task<SearchResultsDto> SearchDirectMessagesAsync(
            Guid userId, 
            string searchTerm,
            Guid? specificConversationId, 
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = from message in _context.Set<DirectMessageReadModel>()
                        join conversation in _context.Set<ConversationReadModel>()
                           on message.ConversationId equals conversation.Id
                        join sender in _context.Set<UserReadModel>()
                           on message.SenderId equals sender.Id
                        where !message.IsDeleted
                           && (conversation.User1Id == userId || conversation.User2Id == userId)
                           && EF.Functions.ILike(message.Content, $"%{searchTerm}%")
                        select new
                        {
                            Message = message,
                            Sender = sender,
                            Conversation = conversation
                        };

            // Filter by specific conversation if provided
            if (specificConversationId.HasValue)
            {
                query = query.Where(x => x.Conversation.Id == specificConversationId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var data = await query
                .OrderByDescending(x => x.Message.CreatedAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // Get other user details
            var results = new List<SearchResultDto>();

            foreach(var item in data)
            {
                var otherUserId=item.Conversation.User1Id==userId
                    ? item.Conversation.User2Id
                    : item.Conversation.User1Id;

                var otherUser = await _context.Set<UserReadModel>()
                    .FirstOrDefaultAsync(u => u.Id == otherUserId, cancellationToken);

                results.Add(new SearchResultDto(
                    item.Message.Id,
                    SearchResultType.DirectMessage,
                    item.Message.Content,
                    HighlightSearchTerm(item.Message.Content, searchTerm),
                    item.Sender.Id,
                    item.Sender.Username,
                    item.Sender.DisplayName,
                    item.Sender.AvatarUrl,
                    item.Message.CreatedAtUtc,
                    null,
                    null,
                    item.Conversation.Id,
                    otherUser?.Id,
                    otherUser?.Username,
                    otherUser?.DisplayName
                ));
            }

            var hasNextPage = totalCount > (pageNumber * pageSize);

            return new SearchResultsDto(
                results,
                totalCount,
                pageNumber,
                pageSize,
                hasNextPage);
        }


        public async Task<SearchResultsDto> SearchMessagesAsync(
            Guid userId,
            string searchTerm,
            SearchScope scope,
            Guid? channelId, 
            Guid? conversationId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var results = new List<SearchResultDto>();

            // Search in channels
            if(scope==SearchScope.All || scope == SearchScope.Channels)
            {
                var channelResults = await SearchChannelMessagesAsync(
                    userId,
                    searchTerm,
                    channelId,
                    pageNumber,
                    pageSize,
                    cancellationToken);

                results.AddRange(channelResults.Results);
            }

            // Search in direct messages
            if(scope==SearchScope.All || scope == SearchScope.DirectMessages)
            {
                var dmResults = await SearchDirectMessagesAsync(
                    userId,
                    searchTerm,
                    conversationId,
                    pageNumber,
                    pageSize,
                    cancellationToken);

                results.AddRange(dmResults.Results);
            }

            // Sort by date descending
            results = results
                .OrderByDescending(r => r.CreatedAtUtc)
                .ToList();

            var totalCount = results.Count;
            var hasNextPage = totalCount > (pageNumber * pageSize);

            return new SearchResultsDto(
                results.Take(pageSize).ToList(),
                totalCount,
                pageNumber,
                pageSize,
                hasNextPage);
        }

        private static string HighlightSearchTerm(string content,string searchTerm)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(searchTerm)) return content;

            // Simple highlight: wrap matching term in <mark> tags
            // Frontend can use this to highlight search results
            return content.Replace(
                searchTerm,
                $"<mark>{searchTerm}</mark>",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}