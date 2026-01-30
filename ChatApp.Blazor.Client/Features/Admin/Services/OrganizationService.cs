using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Organization;

namespace ChatApp.Blazor.Client.Features.Admin.Services;

/// <summary>
/// Implementation of organization hierarchy service
/// Handles organization hierarchy API endpoints
/// </summary>
public class OrganizationService : IOrganizationService
{
    private readonly IApiClient _apiClient;

    public OrganizationService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Gets organization hierarchy (departments and users in tree structure) - GET /api/identity/organization/hierarchy
    /// </summary>
    public async Task<Result<List<OrganizationHierarchyNode>>> GetOrganizationHierarchyAsync()
    {
        return await _apiClient.GetAsync<List<OrganizationHierarchyNode>>("/api/identity/organization/hierarchy");
    }
}
