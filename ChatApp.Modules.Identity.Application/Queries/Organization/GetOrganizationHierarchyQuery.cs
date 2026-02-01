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
                // Get all companies
                var companies = await unitOfWork.Companies
                    .Include(c => c.HeadOfCompany)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

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
                    Level: 0,
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
                    Level: 0,
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

                // Build hierarchy: Company → Departments → Sub-departments → Users
                var result = BuildHierarchy(companies, departments, departmentNodes, userNodes);

                return Result.Success(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving organization hierarchy");
                return Result.Failure<List<OrganizationHierarchyNodeDto>>(
                    "An error occurred while retrieving organization hierarchy");
            }
        }

        private List<OrganizationHierarchyNodeDto> BuildHierarchy(
            List<Domain.Entities.Company> companies,
            List<Domain.Entities.Department> departmentEntities,
            List<OrganizationHierarchyNodeDto> departmentNodes,
            List<OrganizationHierarchyNodeDto> userNodes)
        {
            // Build lookup: departmentId -> head name
            var deptHeadNames = departmentNodes
                .Where(d => d.HeadOfDepartmentId.HasValue && d.HeadOfDepartmentName != null)
                .ToDictionary(d => d.Id, d => d.HeadOfDepartmentName!);

            // Group users by department
            var usersByDepartment = userNodes
                .Where(u => u.DepartmentId.HasValue)
                .GroupBy(u => u.DepartmentId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Group departments by companyId
            var departmentsByCompany = departmentEntities
                .GroupBy(d => d.CompanyId)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());

            // Group child departments by parent
            var childDepartmentsByParent = departmentNodes
                .Where(d => d.ParentDepartmentId.HasValue)
                .GroupBy(d => d.ParentDepartmentId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<OrganizationHierarchyNodeDto>();

            foreach (var company in companies)
            {
                var companyHeadName = company.HeadOfCompany?.FullName;

                // Get root departments for this company (no parent), sorted A-Z
                var companyDeptIds = departmentsByCompany.GetValueOrDefault(company.Id) ?? [];
                var rootDepts = departmentNodes
                    .Where(d => companyDeptIds.Contains(d.Id) && !d.ParentDepartmentId.HasValue)
                    .OrderBy(d => d.Name)
                    .ToList();

                // Build department tree under this company
                var companyChildren = new List<OrganizationHierarchyNodeDto>();
                foreach (var rootDept in rootDepts)
                {
                    var subordinateCount = CalculateSubordinateCount(rootDept, usersByDepartment);
                    var deptWithInfo = rootDept with
                    {
                        SupervisorName = companyHeadName,
                        SubordinateCount = subordinateCount
                    };
                    companyChildren.Add(BuildDepartmentNode(deptWithInfo, 1,
                        childDepartmentsByParent, usersByDepartment, deptHeadNames));
                }

                var companyNode = new OrganizationHierarchyNodeDto(
                    Type: NodeType.Company,
                    Id: company.Id,
                    Name: company.Name,
                    Level: 0,
                    ParentDepartmentId: null,
                    HeadOfDepartmentName: companyHeadName,
                    HeadOfDepartmentId: company.HeadOfCompanyId,
                    UserCount: 0,
                    Email: null,
                    Role: null,
                    IsActive: true,
                    AvatarUrl: null,
                    PositionName: null,
                    DepartmentId: null,
                    CreatedAtUtc: company.CreatedAtUtc,
                    Children: companyChildren,
                    SubordinateCount: 0
                );

                result.Add(companyNode);
            }

            // Add users with no department to root
            var usersWithoutDepartment = userNodes
                .Where(u => !u.DepartmentId.HasValue)
                .Select(u => u with { Level = 0 })
                .ToList();
            result.AddRange(usersWithoutDepartment);

            return result;
        }

        private static OrganizationHierarchyNodeDto BuildDepartmentNode(
            OrganizationHierarchyNodeDto dept,
            int level,
            Dictionary<Guid, List<OrganizationHierarchyNodeDto>> childDepartmentsByParent,
            Dictionary<Guid, List<OrganizationHierarchyNodeDto>> usersByDepartment,
            Dictionary<Guid, string> deptHeadNames)
        {
            var children = new List<OrganizationHierarchyNodeDto>();

            // This dept's head name (for assigning as supervisor to children)
            deptHeadNames.TryGetValue(dept.Id, out var thisDeptHeadName);

            // Recursively add child departments (sorted A-Z)
            if (childDepartmentsByParent.TryGetValue(dept.Id, out var childDepts))
            {
                foreach (var child in childDepts.OrderBy(d => d.Name))
                {
                    var subordinateCount = CalculateSubordinateCount(child, usersByDepartment);
                    // Child dept head's supervisor = this dept's head
                    var childWithInfo = child with
                    {
                        SupervisorName = thisDeptHeadName,
                        SubordinateCount = subordinateCount
                    };
                    children.Add(BuildDepartmentNode(childWithInfo, level + 1,
                        childDepartmentsByParent, usersByDepartment, deptHeadNames));
                }
            }

            // Add users in this department (department head first, then others A-Z)
            if (usersByDepartment.TryGetValue(dept.Id, out var users))
            {
                var sortedUsers = users
                    .OrderByDescending(u => u.Id == dept.HeadOfDepartmentId)
                    .ThenBy(u => u.Name)
                    .ToList();
                foreach (var user in sortedUsers)
                {
                    string? supervisorName;
                    if (user.Id == dept.HeadOfDepartmentId)
                    {
                        // This user IS the dept head - supervisor comes from parent
                        supervisorName = dept.SupervisorName;
                    }
                    else
                    {
                        // Regular employee - supervisor = dept head
                        supervisorName = thisDeptHeadName;
                    }
                    var isDeptHead = user.Id == dept.HeadOfDepartmentId;
                    children.Add(user with { Level = level + 1, SupervisorName = supervisorName, IsDepartmentHead = isDeptHead });
                }
            }

            return dept with { Level = level, Children = children };
        }

        private static int CalculateSubordinateCount(
            OrganizationHierarchyNodeDto dept,
            Dictionary<Guid, List<OrganizationHierarchyNodeDto>> usersByDepartment)
        {
            if (!usersByDepartment.TryGetValue(dept.Id, out var users)) return 0;
            // Count direct employees excluding the department head
            return users.Count(u => u.Id != dept.HeadOfDepartmentId);
        }
    }
}
