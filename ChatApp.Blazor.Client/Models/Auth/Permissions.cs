namespace ChatApp.Blazor.Client.Models.Auth;

/// <summary>
/// Static permission constants for the application.
/// Should match backend permissions exactly.
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
}