using System.Reflection;
using ChatApp.Modules.Identity.Domain.Enums;

namespace ChatApp.Modules.Identity.Domain.Constants
{
    /// <summary>
    /// Static permission constants for the application.
    /// Permissions are hardcoded and should not be created dynamically.
    /// </summary>
    public static class Permissions
    {
        // Identity Module - User Management
        public const string UsersCreate = "Users.Create";
        public const string UsersRead = "Users.Read";
        public const string UsersUpdate = "Users.Update";
        public const string UsersDelete = "Users.Delete";

        // Identity Module - Permission Management (Admin only)
        public const string PermissionsRead = "Permissions.Read";
        public const string PermissionsAssign = "Permissions.Assign";
        public const string PermissionsRevoke = "Permissions.Revoke";

        // Messaging Module - Messages
        public const string MessagesSend = "Messages.Send";
        public const string MessagesRead = "Messages.Read";
        public const string MessagesEdit = "Messages.Edit";
        public const string MessagesDelete = "Messages.Delete";

        // Files Module
        public const string FilesUpload = "Files.Upload";
        public const string FilesDownload = "Files.Download";
        public const string FilesDelete = "Files.Delete";

        // Channels Module
        public const string ChannelsCreate = "Channels.Create";
        public const string ChannelsRead = "Channels.Read";
        public const string ChannelsManage = "Channels.Manage";
        public const string ChannelsDelete = "Channels.Delete";

        /// <summary>
        /// Gets all available permissions in the system
        /// </summary>
        public static IEnumerable<string> GetAll()
        {
            return typeof(Permissions)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => (string)f.GetValue(null)!)
                .ToList();
        }

        /// <summary>
        /// Gets all permissions grouped by module
        /// </summary>
        public static Dictionary<string, List<string>> GetGroupedByModule()
        {
            return GetAll()
                .GroupBy(p => p.Split('.')[0])
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Gets default permissions for a specific role.
        /// Administrator gets all permissions, User gets basic permissions.
        /// </summary>
        public static IEnumerable<string> GetDefaultForRole(Role role)
        {
            return role switch
            {
                Role.Administrator => GetAll(),
                Role.User => new[]
                {
                    // Basic user permissions
                    UsersRead,
                    // Messaging permissions
                    MessagesSend,
                    MessagesRead,
                    MessagesEdit,
                    MessagesDelete,
                    // File permissions
                    FilesUpload,
                    FilesDelete,
                    FilesDownload,
                    // Channel permissions
                    ChannelsRead
                },
                _ => []
            };
        }
    }
}