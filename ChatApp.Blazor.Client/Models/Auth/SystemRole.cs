namespace ChatApp.Blazor.Client.Models.Auth;

/// <summary>
/// Defines the three standard system roles that cannot be deleted or modified.
/// Should match backend SystemRole enum exactly.
/// </summary>
public enum SystemRole
{
    /// <summary>
    /// Administrator role - has ALL permissions in the system
    /// </summary>
    Administrator = 1,

    /// <summary>
    /// Operator role - has elevated permissions including Channels.Manage, Channels.Delete, Files.Delete
    /// Plus all User role permissions
    /// </summary>
    Operator = 2,

    /// <summary>
    /// User role - basic user permissions: Users.Read, Messages.*, Files.Upload, Files.Download
    /// </summary>
    User = 3
}
