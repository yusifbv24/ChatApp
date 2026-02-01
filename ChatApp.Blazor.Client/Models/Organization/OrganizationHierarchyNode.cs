namespace ChatApp.Blazor.Client.Models.Organization;

public enum NodeType
{
    Department,
    User,
    Company
}

public class OrganizationHierarchyNode
{
    public NodeType Type { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsExpanded { get; set; }

    // Department-specific properties
    public Guid? ParentDepartmentId { get; set; }
    public string? HeadOfDepartmentName { get; set; }
    public Guid? HeadOfDepartmentId { get; set; }
    public int UserCount { get; set; }

    // User-specific properties
    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public string? AvatarUrl { get; set; }
    public string? PositionName { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime? CreatedAtUtc { get; set; }

    // Tree structure
    public List<OrganizationHierarchyNode> Children { get; set; } = [];

    // Supervisor & Subordinate info
    public int SubordinateCount { get; set; }
    public string? SupervisorName { get; set; }
    public bool IsDepartmentHead { get; set; }

    // Search/filter helper
    public bool IsVisible { get; set; } = true;
    public bool MatchesSearch { get; set; } = true;
}
