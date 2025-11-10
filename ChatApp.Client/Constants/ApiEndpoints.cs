namespace ChatApp.Client.Constants
{
    public static class ApiEndpoints
    {
        public static class Auth
        {
            public const string Login = "/api/auth/login";

            public const string Logout = "/api/auth/logout";

            public const string Refresh = "/api/auth/refresh";
        }
        public static class Users
        {
            public const string GetCurrentUser = "/api/users/me";

            public const string UpdateCurrentUser = "/api/users/me";

            public const string ChangeCurrentUserPassword = "/api/users/me/change-password";
            public static string GetUsers(int pageNumber = 1, int pageSize = 20) =>
                $"/api/users?pageNumber={pageNumber}&pageSize={pageSize}";
            public static string GetUserById(Guid userId) => $"/api/users/{userId}";

            public const string CreateUser = "/api/users";
            public static string UpdateUser(Guid userId) => $"/api/users/{userId}";
            public static string DeleteUser(Guid userId) => $"/api/users/{userId}";

            public static string AssignRole(Guid userId, Guid roleId) =>
                $"/api/users/{userId}/roles/{roleId}";

            public static string RemoveRole(Guid userId, Guid roleId) =>
                $"/api/users/{userId}/roles/{roleId}";

            public static string AdminChangeUserPassword(Guid userId) =>
                $"/api/users/change-password/{userId}";
        }
        public static class Roles
        {
            public const string GetRoles = "/api/roles";

            public static string GetRoleById(Guid roleId) => $"/api/roles/{roleId}";

            public const string CreateRole = "/api/roles";

            public static string UpdateRole(Guid roleId) => $"/api/roles/{roleId}";

            public static string DeleteRole(Guid roleId) => $"/api/roles/{roleId}";
        }
        public static class Permissions
        {
            public static string GetPermissions(string? module = null) =>
                string.IsNullOrWhiteSpace(module)
                    ? "/api/permissions"
                    : $"/api/permissions?module={module}";

            public static string AssignPermissionToRole(Guid roleId, Guid permissionId) =>
                $"/api/permissions/roles/{roleId}/permissions/{permissionId}";

            public static string RemovePermissionFromRole(Guid roleId, Guid permissionId) =>
                $"/api/permissions/roles/{roleId}/permissions/{permissionId}";
        }
    }
}