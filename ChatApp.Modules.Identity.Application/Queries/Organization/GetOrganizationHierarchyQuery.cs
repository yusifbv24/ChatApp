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
            // Create a dictionary for quick department lookup
            var departmentDict = departmentNodes.ToDictionary(d => d.Id);

            // Find root departments (no parent)
            var rootDepartments = departmentNodes
                .Where(d => !d.ParentDepartmentId.HasValue)
                .ToList();

            // Build tree recursively
            foreach (var dept in departmentNodes)
            {
                var children = new List<OrganizationHierarchyNodeDto>();

                // Add child departments
                var childDepartments = departmentNodes
                    .Where(d => d.ParentDepartmentId == dept.Id)
                    .ToList();

                foreach (var child in childDepartments)
                {
                    // Update level
                    var updatedChild = child with { Level = dept.Level + 1 };
                    children.Add(updatedChild);
                }

                // Add users in this department
                var departmentUsers = userNodes
                    .Where(u => u.DepartmentId == dept.Id)
                    .Select(u => u with { Level = dept.Level + 1 })
                    .ToList();

                children.AddRange(departmentUsers);

                // Update department with children
                if (children.Any())
                {
                    departmentDict[dept.Id] = dept with { Children = children };
                }
            }

            // Add users with no department to root
            var usersWithoutDepartment = userNodes
                .Where(u => !u.DepartmentId.HasValue)
                .Select(u => u with { Level = 0 })
                .ToList();

            // Return root departments + users without department
            var result = rootDepartments
                .Select(d => departmentDict[d.Id])
                .Concat(usersWithoutDepartment)
                .ToList();

            return result;
        }
    }
}
