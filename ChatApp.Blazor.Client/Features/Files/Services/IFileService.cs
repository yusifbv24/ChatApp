using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Files;
using Microsoft.AspNetCore.Components.Forms;

namespace ChatApp.Blazor.Client.Features.Files.Services;

/// <summary>
/// Interface for file management operations
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Upload a profile picture from byte array (automatically resized to 400x400)
    /// Admins can specify targetUserId to upload for other users
    /// </summary>
    Task<Result<FileUploadResult>> UploadProfilePictureAsync(byte[] fileData, string fileName, string contentType, Guid? targetUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a profile picture (automatically resized to 400x400)
    /// Admins can specify targetUserId to upload for other users
    /// </summary>
    Task<Result<FileUploadResult>> UploadProfilePictureAsync(IBrowserFile file, Guid? targetUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a file
    /// </summary>
    Task<Result<FileUploadResult>> UploadFileAsync(IBrowserFile file, Guid? conversationId = null, Guid? channelId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a channel avatar from byte array (stored in /avatars/channels/{channelId}/)
    /// </summary>
    Task<Result<FileUploadResult>> UploadChannelAvatarAsync(byte[] fileData, string fileName, string contentType, Guid channelId, CancellationToken cancellationToken = default);
}