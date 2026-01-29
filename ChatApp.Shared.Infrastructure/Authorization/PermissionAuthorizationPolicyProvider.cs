using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ChatApp.Shared.Infrastructure.Authorization
{
    /// <summary>
    /// Custom policy provider that dynamically creates authorization policies for permission requirements
    /// 
    /// This is needed because we can't pre-register all possible permission combinations as policies.
    /// When a [RequirePermission] attribute is encountered, ASP.NET Core asks this provider for a policy
    /// with the name matching the permissions (e.g., "Channels.Create" or "Channels.Create,Channels.Manage").
    /// 
    /// This provider creates a policy on-the-fly that includes all the specified permission requirements.
    /// </summary>
    public class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets a policy by name. If it's a permission policy, creates it dynamically.
        /// </summary>
        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // First, try to get the policy from the default provider
            // This handles built-in policies like [Authorize(Policy = "SomeStaticPolicy")]
            var policy = await base.GetPolicyAsync(policyName);

            if (policy != null)
                return policy;

            // If no policy was found and the policy name contains permission names
            // (indicated by the format "Permission1,Permission2,..."), create a dynamic policy
            if (string.IsNullOrWhiteSpace(policyName))
                return null;

            // Split the policy name to get individual permissions
            var permissions = policyName.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (permissions.Length == 0)
                return null;

            // Build a policy that requires ALL specified permissions
            var policyBuilder = new AuthorizationPolicyBuilder();
            policyBuilder.RequireAuthenticatedUser(); // User must be authenticated

            // Add a requirement for each permission
            // The user must have ALL permissions to satisfy the policy
            foreach (var permission in permissions)
            {
                policyBuilder.AddRequirements(new PermissionRequirement(permission.Trim()));
            }

            return policyBuilder.Build();
        }
    }
}