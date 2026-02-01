using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Queries.Organization
{
    public record GetOrganizationHierarchyQuery : IRequest<Result<List<OrganizationHierarchyNodeDto>>>;

    public class GetOrganizationHierarchyQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetOrganizationHierarchyQueryHandler> logger)
        : IRequestHandler<GetOrganizationHierarchyQuery, Result<List<OrganizationHierarchyNodeDto>>>
    {
        public async Task<Result<List<OrganizationHierarchyNodeDto>>> Handle(
            GetOrganizationHierarchyQuery query,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get all departments
                var departments = await unitOfWork.Departments
                    .Include(d => d.ParentDepartment)
                    .Include(d => d.HeadOfDepartment)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                // Get all users with their employee data
                var users = await unitOfWork.Users
                    .Include(u => u.Employee)
                        .ThenInclude(e => e!.Position)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                // Count users per department
                var departmentUserCounts = users
                    .Where(u => u.Employee != null && u.Employee.DepartmentId.HasValue)
                    .GroupBy(u => u.Employee!.DepartmentId!.Value)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Build department nodes
                var departmentNodes = departments.Select(d => new OrganizationHierarchyNodeDto(
                    Type: NodeType.Department,
                    Id: d.Id,
                    Name: d.Name,
                    Level: 0, // Will be calculated when building tree
                    ParentDepartmentId: d.ParentDepartmentId,
                    HeadOfDepartmentName: d.HeadOfDepartment?.FullName,
                    HeadOfDepartmentId: d.HeadOfDepartmentId,
                    UserCount: departmentUserCounts.GetValueOrDefault(d.Id, 0),
                    Email: null,
                    Role: null,
                    IsActive: true,
                    AvatarUrl: null,
                    PositionName: null,
                    DepartmentId: null,
                    CreatedAtUtc: d.CreatedAtUtc,
                    Children: new List<OrganizationHierarchyNodeDto>()
                )).ToList();

                // Build user nodes
                var userNodes = users.Select(u => new OrganizationHierarchyNodeDto(
                    Type: NodeType.User,
                    Id: u.Id,
                    Name: u.FullName,
                    Level: 0, // Will be calculated when building tree
                    ParentDepartmentId: null,
                    HeadOfDepartmentName: null,
                    HeadOfDepartmentId: null,
                    UserCount: 0,
                    Email: u.Email,
                    Role: u.Role.ToString(),
                    IsActive: u.IsActive,
                    AvatarUrl: u.AvatarUrl,
                    PositionName: u.Employee?.Position?.Name,
                    DepartmentId: u.Employee?.DepartmentId,
                    CreatedAtUtc: u.CreatedAtUtc,
                    Children: new List<OrganizationHierarchyNodeDto>()
                )).ToList();

                // Build the hierarchy tree
                var rootNodes = BuildHierarchy(departmentNodes, userNodes);

                return Result.Success(rootNodes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving organization hierarchy");
                return Result.Failure<List<OrganizationHierarchyNodeDto>>(
                    "An error occurred while retrieving organization hierarchy");
            }
        }

        private List<OrganizationHierarchyNodeDto> BuildHierarchy(
            List<OrganizationHierarchyNodeDto> departmentNodes,
            List<OrganizationHierarchyNodeDto> userNodes)
        {
            // Group users by department
            var usersByDepartment = userNodes
                .Where(u => u.DepartmentId.HasValue)
                .GroupBy(u => u.DepartmentId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Group child departments by parent
            var childDepartmentsByParent = departmentNodes
                .Where(d => d.ParentDepartmentId.HasValue)
                .GroupBy(d => d.ParentDepartmentId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Find root departments (no parent)
            var rootDepartments = departmentNodes
                .Where(d => !d.ParentDepartmentId.HasValue)
                .ToList();

            // Build tree recursively from roots
            var result = rootDepartments
                .Select(d => BuildNode(d, 0, childDepartmentsByParent, usersByDepartment))
                .ToList();

            // Add users with no department to root
            var usersWithoutDepartment = userNodes
                .Where(u => !u.DepartmentId.HasValue)
                .Select(u => u with { Level = 0 })
                .ToList();

            result.AddRange(usersWithoutDepartment);

            return result;
        }

        private static OrganizationHierarchyNodeDto BuildNode(
            OrganizationHierarchyNodeDto dept,
            int level,
            Dictionary<Guid, List<OrganizationHierarchyNodeDto>> childDepartmentsByParent,
            Dictionary<Guid, List<OrganizationHierarchyNodeDto>> usersByDepartment)
        {
            var children = new List<OrganizationHierarchyNodeDto>();

            // Recursively add child departments
            if (childDepartmentsByParent.TryGetValue(dept.Id, out var childDepts))
            {
                foreach (var child in childDepts)
                {
                    children.Add(BuildNode(child, level + 1, childDepartmentsByParent, usersByDepartment));
                }
            }

            // Add users in this department
            if (usersByDepartment.TryGetValue(dept.Id, out var users))
            {
                children.AddRange(users.Select(u => u with { Level = level + 1 }));
            }

            return dept with { Level = level, Children = children };
        }
    }
}
