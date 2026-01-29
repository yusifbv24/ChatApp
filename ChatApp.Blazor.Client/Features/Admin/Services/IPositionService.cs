using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Organization;

namespace ChatApp.Blazor.Client.Features.Admin.Services;

/// <summary>
/// Interface for position management operations
/// </summary>
public interface IPositionService
{
    Task<Result<List<PositionDto>>> GetAllPositionsAsync();
    Task<Result<List<PositionDto>>> GetPositionsByDepartmentAsync(Guid departmentId);
    Task<Result<Guid>> CreatePositionAsync(CreatePositionRequest request);
    Task<Result> UpdatePositionAsync(Guid positionId, UpdatePositionRequest request);
    Task<Result> DeletePositionAsync(Guid positionId);
}
