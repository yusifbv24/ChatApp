using ChatApp.Modules.Identity.Application.Commands.Login;
using ChatApp.Modules.Identity.Application.Commands.Permisisons;
using ChatApp.Modules.Identity.Application.Commands.RefreshToken;
using ChatApp.Modules.Identity.Application.Commands.Roles;
using ChatApp.Modules.Identity.Application.Commands.Users;
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
            services.AddScoped<LoginCommand>();
            services.AddScoped<RefreshTokenCommand>();
            services.AddScoped<CreateUserCommand>();
            services.AddScoped<UpdateUserCommand>();
            services.AddScoped<DeleteUserCommand>();
            services.AddScoped<AssignRoleCommand>();
            services.AddScoped<CreateRoleCommand>();
            services.AddScoped<AssignPermissionCommandHandler>();

            // Register query handlers
            services.AddScoped<GetUserQueryHandler>();
            services.AddScoped<GetUsersQueryHandler>();
            services.AddScoped<GetRolesQueryHandler>();
            services.AddScoped<GetPermissionsQuery>();

            return services;
        }
    }
}