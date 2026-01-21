using Microsoft.AspNetCore.Authorization;

namespace ChatApp.Shared.Infrastructure.Authorization
{
    /// <summary>
    /// Custom authorization attribute that requires a specific permission
    /// This attribute can be applied to controller actions to enforce permission-based access control
    /// 
    /// Usage example:
    /// [RequirePermission("Groups.Create")]
    /// public async Task<IActionResult> CreateChannel(...)
    /// 
    /// Multiple permissions can be specified if needed:
    /// [RequirePermission("Messages.Send", "Messages.Edit")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// Creates a new permission requirement attribute
        /// </summary>
        /// <param name="permissions">One or more permission names that are required to access this endpoint</param>
        public RequirePermissionAttribute(params string[] permissions)
        {
            if (permissions == null || permissions.Length == 0)
                throw new ArgumentException("At least one permission must be specified", nameof(permissions));

            // Set the Policy property to our custom policy name
            // The policy name includes all required permissions separated by commas
            // This will be used by our PermissionAuthorizationPolicyProvider to create the actual policy
            Policy = string.Join(",", permissions);
        }

        /// <summary>
        /// The permissions required for this endpoint
        /// </summary>
        public string[] Permissions => Policy?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
    }
}