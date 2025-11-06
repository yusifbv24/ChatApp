using ChatApp.Modules.Search.Application.DTOs.Requests;
using ChatApp.Modules.Search.Domain.Enums;

namespace ChatApp.Modules.Search.Application.Interfaces
{
    public interface ISearchRepository
    {
        /// <summary>
        /// Search messages across channels and conversations
        /// </summary>
        Task<SearchResultsDto> SearchMessagesAsync(
            Guid userId,
            string searchTerm,
            SearchScope scope,
            Guid? channelId,
            Guid? conversationId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken=default);



        /// <summary>
        /// Search only in channels user is member of
        /// </summary>
        Task<SearchResultsDto> SearchChannelMessagesAsync(
            Guid userId,
            string searchTerm,
            Guid? specificChannelId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken= default);



        /// <summary>
        /// Search only in user's direct conversations
        /// </summary>
        Task<SearchResultsDto> SearchDirectMessagesAsync(
            Guid userId,
            string searchTerm,
            Guid? specificConversationId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken= default);
    }
}