namespace ChatApp.Modules.Identity.Domain.Enums
{
    /// <summary>
    /// User role enumeration.
    /// Only two roles exist in the system: User (default) and Administrator.
    /// </summary>
    public enum Role
    {
        /// <summary>
        /// Regular user with default permissions.
        /// Default role for all new users.
        /// </summary>
        User = 0,

        /// <summary>
        /// Administrator with all permissions.
        /// Has access to all system features.
        /// </summary>
        Administrator = 1
    }
}