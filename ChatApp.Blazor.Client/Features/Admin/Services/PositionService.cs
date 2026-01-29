using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Organization;

namespace ChatApp.Blazor.Client.Features.Admin.Services;

/// <summary>
/// Implementation of position management service
/// Handles all position-related API endpoints
/// </summary>
public class PositionService : IPositionService
{
    private readonly IApiClient _apiClient;

    public PositionService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Gets all positions - GET /api/identity/positions
    /// </summary>
    public async Task<Result<List<PositionDto>>> GetAllPositionsAsync()
    {
        return await _apiClient.GetAsync<List<PositionDto>>("/api/identity/positions");
    }

    /// <summary>
    /// Gets positions by department ID - GET /api/identity/positions/department/{departmentId}
    /// </summary>
    public async Task<Result<List<PositionDto>>> GetPositionsByDepartmentAsync(Guid departmentId)
    {
        return await _apiClient.GetAsync<List<PositionDto>>($"/api/identity/positions/department/{departmentId}");
    }

    /// <summary>
    /// Creates a new position - POST /api/identity/positions
    /// </summary>
    public async Task<Result<Guid>> CreatePositionAsync(CreatePositionRequest request)
    {
        return await _apiClient.PostAsync<Guid>("/api/identity/positions", request);
    }

    /// <summary>
    /// Updates position information - PUT /api/identity/positions/{positionId}
    /// </summary>
    public async Task<Result> UpdatePositionAsync(Guid positionId, UpdatePositionRequest request)
    {
        return await _apiClient.PutAsync($"/api/identity/positions/{positionId}", request);
    }

    /// <summary>
    /// Deletes a position - DELETE /api/identity/positions/{positionId}
    /// </summary>
    public async Task<Result> DeletePositionAsync(Guid positionId)
    {
        return await _apiClient.DeleteAsync($"/api/identity/positions/{positionId}");
    }
}
