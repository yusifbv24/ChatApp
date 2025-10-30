using ChatApp.Modules.Identity.Application.Commands.AssignPermission;
using ChatApp.Modules.Identity.Application.Commands.AssignRole;
using ChatApp.Modules.Identity.Application.Commands.CreateRole;
using ChatApp.Modules.Identity.Application.Commands.CreateUser;
using ChatApp.Modules.Identity.Application.Commands.DeleteUser;
using ChatApp.Modules.Identity.Application.Commands.Login;
using ChatApp.Modules.Identity.Application.Commands.RefreshToken;
using ChatApp.Modules.Identity.Application.Commands.UpdateUser;
using ChatApp.Modules.Identity.Application.Queries.GetPermissions;
using ChatApp.Modules.Identity.Application.Queries.GetRoles;
using ChatApp.Modules.Identity.Application.Queries.GetUser;
using ChatApp.Modules.Identity.Application.Queries.GetUsers;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Modules.Identity.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
        {
            // Register command handlers
            services.AddScoped<LoginCommandHandler>();
            services.AddScoped<RefreshTokenCommandHandler>();
            services.AddScoped<CreateUserCommandHandler>();
            services.AddScoped<UpdateUserCommandHandler>();
            services.AddScoped<DeleteUserCommandHandler>();
            services.AddScoped<AssignRoleCommandHandler>();
            services.AddScoped<CreateRoleCommandHandler>();
            services.AddScoped<AssignPermissionCommandHandler>();

            // Register query handlers
            services.AddScoped<GetUserQueryHandler>();
            services.AddScoped<GetUsersQueryHandler>();
            services.AddScoped<GetRolesQueryHandler>();
            services.AddScoped<GetPermissionsQueryHandler>();

            return services;
        }
    }
}