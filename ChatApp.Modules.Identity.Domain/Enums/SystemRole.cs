namespace ChatApp.Modules.Identity.Domain.Enums
{
    /// <summary>
    /// Defines the three standard system roles that cannot be deleted or modified
    /// </summary>
    public enum SystemRole
    {
        /// <summary>
        /// Administrator role - has ALL permissions in the system
        /// </summary>
        Administrator = 1,

        /// <summary>
        /// Operator role - has elevated permissions including Groups.Manage, Groups.Delete, Files.Delete
        /// Plus all User role permissions
        /// </summary>
        Operator = 2,

        /// <summary>
        /// User role - basic user permissions: Users.Read, Messages.*, Files.Upload, Files.Download
        /// </summary>
        User = 3
    }
}