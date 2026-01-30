using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Organization;

namespace ChatApp.Blazor.Client.Features.Admin.Services;

/// <summary>
/// Interface for department management operations
/// </summary>
public interface IDepartmentService
{
    Task<Result<List<DepartmentDto>>> GetAllDepartmentsAsync();
    Task<Result<DepartmentDto>> GetDepartmentByIdAsync(Guid departmentId);
    Task<Result<CreateDepartmentResponse>> CreateDepartmentAsync(CreateDepartmentRequest request);
    Task<Result> UpdateDepartmentAsync(Guid departmentId, UpdateDepartmentRequest request);
    Task<Result> DeleteDepartmentAsync(Guid departmentId);
    Task<Result> AssignDepartmentHeadAsync(Guid departmentId, Guid userId);
    Task<Result> RemoveDepartmentHeadAsync(Guid departmentId);
}
