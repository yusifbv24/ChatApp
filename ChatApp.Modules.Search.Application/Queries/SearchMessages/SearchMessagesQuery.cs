using ChatApp.Modules.Search.Application.DTOs.Requests;
using ChatApp.Modules.Search.Application.Interfaces;
using ChatApp.Modules.Search.Domain.Enums;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Search.Application.Queries.SearchMessages
{
    public record SearchMessagesQuery(
        Guid UserId,
        string SearchTerm,
        SearchScope Scope=SearchScope.All,
        Guid? ChannelId=null,
        Guid? ConversationId=null,
        int PageNumber=1,
        int PageSize=20
    ):IRequest<Result<SearchResultsDto>>;



    public class SearchMessagesQueryValidator : AbstractValidator<SearchMessagesQuery>
    {
        public SearchMessagesQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.SearchTerm)
                .NotEmpty().WithMessage("Search term is required")
                .MinimumLength(2).WithMessage("Search term must be at least 2 characters")
                .MaximumLength(200).WithMessage("Search term cannot exceed 200 characters");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");

            RuleFor(x => x.ChannelId)
                .NotEmpty()
                .When(x => x.Scope == SearchScope.SpecificChannel)
                .WithMessage("Channel ID is required when searching specific channel");

            RuleFor(x => x.ConversationId)
                .NotEmpty()
                .When(x => x.Scope == SearchScope.SpecificConversation)
                .WithMessage("Conversation ID is required when searching specific conversation");
        }
    }


    public class SearchMessagesQueryHandler : IRequestHandler<SearchMessagesQuery, Result<SearchResultsDto>>
    {
        private readonly ISearchRepository _searchRepository;
        private readonly ILogger<SearchMessagesQueryHandler> _logger;

        public SearchMessagesQueryHandler(
            ISearchRepository searchRepository,
            ILogger<SearchMessagesQueryHandler> logger)
        {
            _searchRepository = searchRepository;
            _logger = logger;
        }


        public async Task<Result<SearchResultsDto>> Handle(
            SearchMessagesQuery request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation(
                    "Searching messages: User={UserId}, Term={SearchTerm}, Scope={Scope}",
                    request.UserId,
                    request.SearchTerm,
                    request.Scope);

                SearchResultsDto results = request.Scope switch
                {
                    SearchScope.Channels => await _searchRepository.SearchChannelMessagesAsync(
                        request.UserId,
                        request.SearchTerm,
                        null,
                        request.PageNumber,
                        request.PageSize,
                        cancellationToken),

                    SearchScope.DirectMessages => await _searchRepository.SearchDirectMessagesAsync(
                        request.UserId,
                        request.SearchTerm,
                        null,
                        request.PageNumber,
                        request.PageSize),

                    SearchScope.SpecificChannel=> await _searchRepository.SearchChannelMessagesAsync(
                        request.UserId,
                        request.SearchTerm,
                        request.ChannelId,
                        request.PageNumber,
                        request.PageSize),

                    SearchScope.SpecificConversation=>await _searchRepository.SearchDirectMessagesAsync(
                        request.UserId,
                        request.SearchTerm,
                        request.ConversationId,
                        request.PageNumber,
                        request.PageSize),

                    _=> await _searchRepository.SearchMessagesAsync(
                        request.UserId,
                        request.SearchTerm,
                        request.Scope,
                        request.ChannelId,
                        request.ConversationId,
                        request.PageNumber,
                        request.PageSize)
                };

                _logger?.LogInformation(
                    "Search completed: Found {Count} results",
                    results.Results.Count);

                return Result.Success(results);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching messages");
                return Result.Failure<SearchResultsDto>("An error occurred while searching messages");
            }
        }
    }
}