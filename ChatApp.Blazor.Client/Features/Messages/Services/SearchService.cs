using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Search;

namespace ChatApp.Blazor.Client.Features.Messages.Services
{
    public class SearchService(IApiClient apiClient) : ISearchService
    {
        public async Task<Result<SearchResultsDto>> SearchInConversationAsync(
            Guid conversationId,
            string searchTerm,
            int page = 1,
            int pageSize = 50)
        {
            var url = $"/api/search?q={Uri.EscapeDataString(searchTerm)}&scope=4&conversationId={conversationId}&page={page}&pageSize={pageSize}";
            return await apiClient.GetAsync<SearchResultsDto>(url);
        }

        public async Task<Result<SearchResultsDto>> SearchInChannelAsync(
            Guid channelId,
            string searchTerm,
            int page = 1,
            int pageSize = 50)
        {
            var url = $"/api/search?q={Uri.EscapeDataString(searchTerm)}&scope=3&channelId={channelId}&page={page}&pageSize={pageSize}";
            return await apiClient.GetAsync<SearchResultsDto>(url);
        }
    }
}
