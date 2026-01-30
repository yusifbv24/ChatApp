namespace ChatApp.Modules.Identity.Application.DTOs.Responses
{
    public enum NodeType
    {
        Department,
        User
    }

    public record OrganizationHierarchyNodeDto(
        NodeType Type,
        Guid Id,
        string Name,
        int Level,

        // Department-specific properties
        Guid? ParentDepartmentId,
        string? HeadOfDepartmentName,
        Guid? HeadOfDepartmentId,
        int UserCount,

        // User-specific properties
        string? Email,
        string? Role,
        bool IsActive,
        string? AvatarUrl,
        string? PositionName,
        Guid? DepartmentId,
        DateTime? CreatedAtUtc,

        // Tree structure
        List<OrganizationHierarchyNodeDto> Children
    );
}
