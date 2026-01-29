using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Organization;

namespace ChatApp.Blazor.Client.Features.Admin.Services;

/// <summary>
/// Implementation of department management service
/// Handles all department-related API endpoints
/// </summary>
public class DepartmentService : IDepartmentService
{
    private readonly IApiClient _apiClient;

    public DepartmentService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Gets all departments - GET /api/identity/departments
    /// </summary>
    public async Task<Result<List<DepartmentDto>>> GetAllDepartmentsAsync()
    {
        return await _apiClient.GetAsync<List<DepartmentDto>>("/api/identity/departments");
    }

    /// <summary>
    /// Gets department by ID - GET /api/identity/departments/{departmentId}
    /// </summary>
    public async Task<Result<DepartmentDto>> GetDepartmentByIdAsync(Guid departmentId)
    {
        return await _apiClient.GetAsync<DepartmentDto>($"/api/identity/departments/{departmentId}");
    }

    /// <summary>
    /// Creates a new department - POST /api/identity/departments
    /// </summary>
    public async Task<Result<Guid>> CreateDepartmentAsync(CreateDepartmentRequest request)
    {
        return await _apiClient.PostAsync<Guid>("/api/identity/departments", request);
    }

    /// <summary>
    /// Updates department information - PUT /api/identity/departments/{departmentId}
    /// </summary>
    public async Task<Result> UpdateDepartmentAsync(Guid departmentId, UpdateDepartmentRequest request)
    {
        return await _apiClient.PutAsync($"/api/identity/departments/{departmentId}", request);
    }

    /// <summary>
    /// Deletes a department - DELETE /api/identity/departments/{departmentId}
    /// </summary>
    public async Task<Result> DeleteDepartmentAsync(Guid departmentId)
    {
        return await _apiClient.DeleteAsync($"/api/identity/departments/{departmentId}");
    }

    /// <summary>
    /// Assigns a user as department head - POST /api/identity/departments/{departmentId}/assign-head
    /// </summary>
    public async Task<Result> AssignDepartmentHeadAsync(Guid departmentId, Guid userId)
    {
        return await _apiClient.PostAsync($"/api/identity/departments/{departmentId}/assign-head", new { UserId = userId });
    }

    /// <summary>
    /// Removes department head - DELETE /api/identity/departments/{departmentId}/remove-head
    /// </summary>
    public async Task<Result> RemoveDepartmentHeadAsync(Guid departmentId)
    {
        return await _apiClient.DeleteAsync($"/api/identity/departments/{departmentId}/remove-head");
    }
}
