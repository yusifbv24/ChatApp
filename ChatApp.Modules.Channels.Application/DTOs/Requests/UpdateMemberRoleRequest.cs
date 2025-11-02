using ChatApp.Modules.Channels.Domain.Enums;

namespace ChatApp.Modules.Channels.Application.DTOs.Requests
{
    public record UpdateMemberRoleRequest(MemberRole NewRole);
}