using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Organization;

public class UpdatePositionRequest
{
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Position name must be between 2 and 100 characters")]
    public string? Name { get; set; }

    public Guid? DepartmentId { get; set; }

    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters")]
    public string? Description { get; set; }
}