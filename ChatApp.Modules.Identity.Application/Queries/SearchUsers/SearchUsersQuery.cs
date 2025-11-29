using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.SearchUsers;

public record SearchUsersQuery(string SearchTerm) : IRequest<Result<List<UserDto>>>;

public class SearchUsersQueryHandler(
    IUnitOfWork unitOfWork,
    ILogger<SearchUsersQueryHandler> logger) : IRequestHandler<SearchUsersQuery, Result<List<UserDto>>>
{
    public async Task<Result<List<UserDto>>> Handle(
        SearchUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query.SearchTerm) || query.SearchTerm.Length < 2)
            {
                return Result.Success(new List<UserDto>());
            }

            var searchTerm = query.SearchTerm.ToLower();

            var users = await unitOfWork.Users
                .Where(u => u.IsActive &&
                    (u.Username.ToLower().Contains(searchTerm) ||
                     u.DisplayName.ToLower().Contains(searchTerm)))
                .Take(20)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var userDtos = users.Select(u => new UserDto(
                    u.Id,
                    u.Username,
                    u.Email,
                    u.DisplayName,
                    u.AvatarUrl,
                    u.Notes,
                    u.CreatedBy,
                    u.IsActive,
                    u.IsAdmin,
                    u.CreatedAtUtc,
                    []
                ))
                .ToList();

            return Result.Success(userDtos);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error searching users with term: {SearchTerm}", query.SearchTerm);
            return Result.Failure<List<UserDto>>("An error occurred while searching users");
        }
    }
}
