using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Search;

namespace ChatApp.Blazor.Client.Features.Messages.Services
{
    public interface ISearchService
    {
        Task<Result<SearchResultsDto>> SearchInConversationAsync(
            Guid conversationId,
            string searchTerm,
            int page = 1,
            int pageSize = 50);

        Task<Result<SearchResultsDto>> SearchInChannelAsync(
            Guid channelId,
            string searchTerm,
            int page = 1,
            int pageSize = 50);
    }
}
