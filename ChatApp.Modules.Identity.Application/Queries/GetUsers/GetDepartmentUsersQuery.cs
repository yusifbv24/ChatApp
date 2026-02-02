using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.GetUsers;

/// <summary>
/// Returns paginated list of department colleagues for conversation sidebar.
/// Normal user → same department only.
/// Head of department → own department + all subdepartments (recursive).
/// Admin/SuperAdmin → all users.
/// </summary>
public record GetDepartmentUsersQuery(
    Guid CurrentUserId,
    int PageNumber,
    int PageSize,
    string? SearchTerm,
    List<Guid>? ExcludeUserIds = null
) : IRequest<Result<PagedResult<DepartmentUserDto>>>;

public class GetDepartmentUsersQueryHandler(
    IUnitOfWork unitOfWork,
    ILogger<GetDepartmentUsersQueryHandler> logger)
    : IRequestHandler<GetDepartmentUsersQuery, Result<PagedResult<DepartmentUserDto>>>
{
    private const int MaxPageSize = 50;

    public async Task<Result<PagedResult<DepartmentUserDto>>> Handle(
        GetDepartmentUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pageSize = Math.Min(query.PageSize, MaxPageSize);
            var skip = (query.PageNumber - 1) * pageSize;

            // Get current user with employee info
            var currentUser = await unitOfWork.Users
                .Include(u => u.Employee)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == query.CurrentUserId, cancellationToken);

            if (currentUser == null)
                return Result.Failure<PagedResult<DepartmentUserDto>>("User not found");

            // Build base query: active users, exclude self
            var usersQuery = unitOfWork.Users
                .Where(u => u.IsActive && u.Id != query.CurrentUserId);

            // Exclude users who already have conversations
            if (query.ExcludeUserIds is { Count: > 0 })
            {
                usersQuery = usersQuery.Where(u => !query.ExcludeUserIds.Contains(u.Id));
            }

            // DEPARTMENT AUTHORIZATION LOGIC
            if (!currentUser.IsAdmin && !currentUser.IsSuperAdmin)
            {
                var departmentId = currentUser.Employee?.DepartmentId;
                if (departmentId == null)
                    return Result.Success(PagedResult<DepartmentUserDto>.Create([], query.PageNumber, pageSize, 0));

                // Check if current user is head of any department
                var isHeadOfDepartment = await unitOfWork.Departments
                    .AnyAsync(d => d.HeadOfDepartmentId == query.CurrentUserId, cancellationToken);

                if (isHeadOfDepartment)
                {
                    // Get all departments where this user is head
                    var headDepartmentIds = await unitOfWork.Departments
                        .Where(d => d.HeadOfDepartmentId == query.CurrentUserId)
                        .Select(d => d.Id)
                        .ToListAsync(cancellationToken);

                    // Recursively get all descendant department IDs
                    var allDepartmentIds = new HashSet<Guid>(headDepartmentIds);
                    await GetDescendantDepartmentIdsAsync(headDepartmentIds, allDepartmentIds, cancellationToken);

                    // Also include current user's own department
                    allDepartmentIds.Add(departmentId.Value);

                    usersQuery = usersQuery.Where(u =>
                        u.Employee != null && u.Employee.DepartmentId != null &&
                        allDepartmentIds.Contains(u.Employee.DepartmentId.Value));
                }
                else
                {
                    // Normal employee — only same department
                    usersQuery = usersQuery.Where(u =>
                        u.Employee != null && u.Employee.DepartmentId == departmentId);
                }
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(query.SearchTerm) && query.SearchTerm.Length >= 2)
            {
                var term = query.SearchTerm.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FirstName.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                    u.LastName.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                    u.Email.Contains(term, StringComparison.CurrentCultureIgnoreCase));
            }

            // Get total count
            var totalCount = await usersQuery.CountAsync(cancellationToken);

            // Get paginated results
            var users = await usersQuery
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                .Skip(skip)
                .Take(pageSize)
                .Select(u => new DepartmentUserDto(
                    u.Id,
                    u.FirstName + " " + u.LastName,
                    u.Email,
                    u.AvatarUrl,
                    u.Employee != null && u.Employee.Position != null ? u.Employee.Position.Name : null,
                    u.Employee != null ? u.Employee.DepartmentId : null,
                    u.Employee != null && u.Employee.Department != null ? u.Employee.Department.Name : null
                ))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return Result.Success(PagedResult<DepartmentUserDto>.Create(users, query.PageNumber, pageSize, totalCount));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving department users for user {UserId}", query.CurrentUserId);
            return Result.Failure<PagedResult<DepartmentUserDto>>("An error occurred while retrieving department users");
        }
    }

    /// <summary>
    /// Recursively gets all descendant department IDs using breadth-first traversal.
    /// </summary>
    private async Task GetDescendantDepartmentIdsAsync(
        List<Guid> parentIds,
        HashSet<Guid> allIds,
        CancellationToken cancellationToken)
    {
        var childIds = await unitOfWork.Departments
            .Where(d => d.ParentDepartmentId != null && parentIds.Contains(d.ParentDepartmentId.Value))
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        if (childIds.Count == 0) return;

        foreach (var id in childIds)
            allIds.Add(id);

        // Recurse for next level
        await GetDescendantDepartmentIdsAsync(childIds, allIds, cancellationToken);
    }
}
