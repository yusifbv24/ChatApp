using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Organization;

namespace ChatApp.Blazor.Client.Features.Admin.Services;

/// <summary>
/// Interface for organization hierarchy operations
/// </summary>
public interface IOrganizationService
{
    Task<Result<List<OrganizationHierarchyNode>>> GetOrganizationHierarchyAsync();
}
