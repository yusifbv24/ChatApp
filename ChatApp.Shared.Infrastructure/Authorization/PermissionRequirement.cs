using Microsoft.AspNetCore.Authorization;

namespace ChatApp.Shared.Infrastructure.Authorization
{
    /// <summary>
    /// Represents a permission requirement for authorization
    /// This class defines what needs to be checked - in our case, a specific permission
    /// </summary>
    public class PermissionRequirement:IAuthorizationRequirement
    {
        /// <summary>
        /// The name of the permission required (e.g., "Channels.Create", "Messages.Send")
        /// </summary>
        public string PermissionName { get; }

        public PermissionRequirement(string permissionName)
        {
            if (string.IsNullOrWhiteSpace(permissionName))
                throw new ArgumentException("Permission name cannot be empty", nameof(permissionName));

            PermissionName = permissionName;
        }
    }
}