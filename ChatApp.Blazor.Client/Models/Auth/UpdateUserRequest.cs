using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Auth;

/// <summary>
/// Request model for updating user information
/// </summary>
public class UpdateUserRequest
{
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
    public string? FirstName { get; set; }

    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
    public string? LastName { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? Email { get; set; }

    public Role? Role { get; set; }

    public Guid? PositionId { get; set; }

    public string? AvatarUrl { get; set; }

    [StringLength(500, ErrorMessage = "About me must not exceed 500 characters")]
    public string? AboutMe { get; set; }

    private DateTime? _dateOfBirth;
    public DateTime? DateOfBirth
    {
        get => _dateOfBirth;
        set => _dateOfBirth = value.HasValue && value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value;
    }

    [Phone(ErrorMessage = "Invalid phone number")]
    public string? WorkPhone { get; set; }

    private DateTime? _hiringDate;
    public DateTime? HiringDate
    {
        get => _hiringDate;
        set => _hiringDate = value.HasValue && value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value;
    }
}