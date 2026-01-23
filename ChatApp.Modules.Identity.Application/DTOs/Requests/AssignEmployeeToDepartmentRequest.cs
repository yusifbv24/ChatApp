namespace ChatApp.Modules.Identity.Application.DTOs.Requests
{
    public record AssignEmployeeToDepartmentRequest(
        Guid DepartmentId,
        Guid? SupervisorId = null,
        Guid? HeadOfDepartmentId = null
    );
}