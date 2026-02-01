using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Organization;

public class CreateDepartmentRequest
{
    [Required(ErrorMessage = "Department name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Department name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    public Guid CompanyId { get; set; }

    public Guid? ParentDepartmentId { get; set; }
}