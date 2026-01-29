namespace ChatApp.Blazor.Client.Models.Auth;

/// <summary>
/// Defines the two main user roles in the system.
/// Should match backend Role enum exactly.
/// </summary>
public enum Role
{
    /// <summary>
    /// Regular user with basic permissions
    /// </summary>
    User = 0,

    /// <summary>
    /// Administrator with full system access
    /// </summary>
    Administrator = 1
}
