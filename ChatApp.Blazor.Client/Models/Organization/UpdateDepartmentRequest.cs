using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Organization;

public class UpdateDepartmentRequest
{
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Department name must be between 2 and 100 characters")]
    public string? Name { get; set; }

    public Guid? ParentDepartmentId { get; set; }
}