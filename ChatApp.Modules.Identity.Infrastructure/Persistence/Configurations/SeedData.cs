using ChatApp.Modules.Identity.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.Configurations
{
    public static class SeedData
    {
        // Fixed GUIDs for seeding (deterministic for migrations)
        private static readonly Guid AdministratorRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid OperatorRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid UserRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        // Fixed timestamp for deterministic seeding
        private static readonly DateTime SeedTimestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Permission IDs - Users module
        private static readonly Guid UsersReadId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        private static readonly Guid UsersCreateId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        private static readonly Guid UsersUpdateId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        private static readonly Guid UsersDeleteId = Guid.Parse("10000000-0000-0000-0000-000000000004");

        // Permission IDs - Messages module
        private static readonly Guid MessagesSendId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        private static readonly Guid MessagesReadId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        private static readonly Guid MessagesEditId = Guid.Parse("20000000-0000-0000-0000-000000000003");
        private static readonly Guid MessagesDeleteId = Guid.Parse("20000000-0000-0000-0000-000000000004");

        // Permission IDs - Files module
        private static readonly Guid FilesUploadId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        private static readonly Guid FilesDownloadId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        private static readonly Guid FilesDeleteId = Guid.Parse("30000000-0000-0000-0000-000000000003");

        // Permission IDs - Groups module
        private static readonly Guid GroupsManageId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        private static readonly Guid GroupsDeleteId = Guid.Parse("40000000-0000-0000-0000-000000000002");

        // Permission IDs - Roles module
        private static readonly Guid RolesManageId = Guid.Parse("50000000-0000-0000-0000-000000000001");

        public static void Seed(ModelBuilder modelBuilder)
        {

            // ====== SEED PERMISSIONS ======
            var permissions = new List<object>
            {
                // Users module
                CreatePermission(UsersReadId, "Users.Read", "View users", "Users", SeedTimestamp),
                CreatePermission(UsersCreateId, "Users.Create", "Create new users", "Users", SeedTimestamp),
                CreatePermission(UsersUpdateId, "Users.Update", "Update existing users", "Users", SeedTimestamp),
                CreatePermission(UsersDeleteId, "Users.Delete", "Delete users", "Users", SeedTimestamp),

                // Messages module
                CreatePermission(MessagesSendId, "Messages.Send", "Send messages", "Messages", SeedTimestamp),
                CreatePermission(MessagesReadId, "Messages.Read", "Read messages", "Messages", SeedTimestamp),
                CreatePermission(MessagesEditId, "Messages.Edit", "Edit own messages", "Messages", SeedTimestamp),
                CreatePermission(MessagesDeleteId, "Messages.Delete", "Delete own messages", "Messages", SeedTimestamp),

                // Files module
                CreatePermission(FilesUploadId, "File.Upload", "Upload files", "Files", SeedTimestamp),
                CreatePermission(FilesDownloadId, "File.Download", "Download files", "Files", SeedTimestamp),
                CreatePermission(FilesDeleteId, "File.Delete", "Delete files", "Files", SeedTimestamp),

                // Groups module
                CreatePermission(GroupsManageId, "Groups.Manage", "Manage groups", "Groups", SeedTimestamp),
                CreatePermission(GroupsDeleteId, "Groups.Delete", "Delete groups", "Groups", SeedTimestamp),

                // Roles module
                CreatePermission(RolesManageId, "Roles.Manage", "Manage roles and permissions", "Roles", SeedTimestamp)
            };

            modelBuilder.Entity<Permission>().HasData(permissions);

            // ====== SEED ROLES ======
            var roles = new List<object>
            {
                new
                {
                    Id = AdministratorRoleId,
                    Name = "Administrator",
                    Description = "System administrator with full access to all features",
                    IsSystemRole = true,
                    SystemRoleType = SystemRole.Administrator,
                    CreatedAtUtc = SeedTimestamp,
                    UpdatedAtUtc = SeedTimestamp
                },
                new
                {
                    Id = OperatorRoleId,
                    Name = "Operator",
                    Description = "Operator with elevated permissions including group and file management",
                    IsSystemRole = true,
                    SystemRoleType = SystemRole.Operator,
                    CreatedAtUtc = SeedTimestamp,
                    UpdatedAtUtc = SeedTimestamp
                },
                new
                {
                    Id = UserRoleId,
                    Name = "User",
                    Description = "Standard user with basic messaging and file access",
                    IsSystemRole = true,
                    SystemRoleType = SystemRole.User,
                    CreatedAtUtc = SeedTimestamp,
                    UpdatedAtUtc = SeedTimestamp
                }
            };

            modelBuilder.Entity<Role>().HasData(roles);

            // ====== SEED ROLE-PERMISSION MAPPINGS ======
            var rolePermissions = new List<object>();

            // Administrator - ALL PERMISSIONS
            rolePermissions.AddRange(new[]
            {
                // Users
                CreateRolePermission(AdministratorRoleId, UsersReadId, SeedTimestamp),
                CreateRolePermission(AdministratorRoleId, UsersCreateId, SeedTimestamp),
                CreateRolePermission(AdministratorRoleId, UsersUpdateId, SeedTimestamp),
                CreateRolePermission(AdministratorRoleId, UsersDeleteId, SeedTimestamp),
                // Messages
                CreateRolePermission(AdministratorRoleId, MessagesSendId, SeedTimestamp),
                CreateRolePermission(AdministratorRoleId, MessagesReadId, SeedTimestamp),
                CreateRolePermission(AdministratorRoleId, MessagesEditId, SeedTimestamp),
                CreateRolePermission(AdministratorRoleId, MessagesDeleteId, SeedTimestamp),
                // Files
                CreateRolePermission(AdministratorRoleId, FilesUploadId, SeedTimestamp),
                CreateRolePermission(AdministratorRoleId, FilesDownloadId, SeedTimestamp),
                CreateRolePermission(AdministratorRoleId, FilesDeleteId, SeedTimestamp),
                // Groups
                CreateRolePermission(AdministratorRoleId, GroupsManageId, SeedTimestamp),
                CreateRolePermission(AdministratorRoleId, GroupsDeleteId, SeedTimestamp),
                // Roles
                CreateRolePermission(AdministratorRoleId, RolesManageId, SeedTimestamp)
            });

            // Operator - Groups.Manage, Groups.Delete, Files.Delete + All User permissions
            rolePermissions.AddRange(new[]
            {
                CreateRolePermission(OperatorRoleId, GroupsManageId, SeedTimestamp),
                CreateRolePermission(OperatorRoleId, GroupsDeleteId, SeedTimestamp),
                CreateRolePermission(OperatorRoleId, FilesDeleteId, SeedTimestamp),
                // User permissions
                CreateRolePermission(OperatorRoleId, UsersReadId, SeedTimestamp),
                CreateRolePermission(OperatorRoleId, MessagesSendId, SeedTimestamp),
                CreateRolePermission(OperatorRoleId, MessagesReadId, SeedTimestamp),
                CreateRolePermission(OperatorRoleId, MessagesEditId, SeedTimestamp),
                CreateRolePermission(OperatorRoleId, MessagesDeleteId, SeedTimestamp),
                CreateRolePermission(OperatorRoleId, FilesUploadId, SeedTimestamp),
                CreateRolePermission(OperatorRoleId, FilesDownloadId, SeedTimestamp)
            });

            // User - Users.Read, Messages.*, Files.Upload, Files.Download
            rolePermissions.AddRange(new[]
            {
                CreateRolePermission(UserRoleId, UsersReadId, SeedTimestamp),
                CreateRolePermission(UserRoleId, MessagesSendId, SeedTimestamp),
                CreateRolePermission(UserRoleId, MessagesReadId, SeedTimestamp),
                CreateRolePermission(UserRoleId, MessagesEditId, SeedTimestamp),
                CreateRolePermission(UserRoleId, MessagesDeleteId, SeedTimestamp),
                CreateRolePermission(UserRoleId, FilesUploadId, SeedTimestamp),
                CreateRolePermission(UserRoleId, FilesDownloadId, SeedTimestamp)
            });

            modelBuilder.Entity<RolePermission>().HasData(rolePermissions);
        }

        private static object CreatePermission(Guid id, string name, string description, string module, DateTime now)
        {
            return new
            {
                Id = id,
                Name = name,
                Description = description,
                Module = module,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
        }

        private static object CreateRolePermission(Guid roleId, Guid permissionId, DateTime timestamp)
        {
            // Generate deterministic GUID based on role and permission IDs
            var combinedBytes = roleId.ToByteArray().Concat(permissionId.ToByteArray()).ToArray();
            var hash = System.Security.Cryptography.MD5.HashData(combinedBytes);
            var deterministicGuid = new Guid(hash);

            return new
            {
                Id = deterministicGuid,
                RoleId = roleId,
                PermissionId = permissionId,
                CreatedAtUtc = timestamp,
                UpdatedAtUtc = timestamp
            };
        }
    }
}
