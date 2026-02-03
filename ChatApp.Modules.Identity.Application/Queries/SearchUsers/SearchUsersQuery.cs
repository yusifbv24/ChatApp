using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.SearchUsers;

public record SearchUsersQuery(string SearchTerm) : IRequest<Result<List<UserSearchResultDto>>>;

public class SearchUsersQueryHandler(
    IUnitOfWork unitOfWork,
    ILogger<SearchUsersQueryHandler> logger) : IRequestHandler<SearchUsersQuery, Result<List<UserSearchResultDto>>>
{
    private const int MaxResults = 20;

    public async Task<Result<List<UserSearchResultDto>>> Handle(
        SearchUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query.SearchTerm) || query.SearchTerm.Length < 2)
            {
                return Result.Success(new List<UserSearchResultDto>());
            }

            var searchTerm = query.SearchTerm.ToLower();

            var users = await unitOfWork.Users
                .Where(u => u.IsActive &&
                    (EF.Functions.Like(u.FirstName.ToLower(), $"%{searchTerm}%") ||
                     EF.Functions.Like(u.LastName.ToLower(), $"%{searchTerm}%") ||
                     EF.Functions.Like(u.Email.ToLower(), $"%{searchTerm}%")))
                .Select(u => new UserSearchResultDto(
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.AvatarUrl,
                    u.Employee != null && u.Employee.Position != null ? u.Employee.Position.Name : null))
                .Take(MaxResults)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return Result.Success(users);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching users with term: {SearchTerm}", query.SearchTerm);
            return Result.Failure<List<UserSearchResultDto>>("An error occurred while searching users");
        }
    }
}