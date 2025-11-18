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
    /// Uploads a file
    /// POST /api/files/upload
    /// </summary>
    Task<Result<FileUploadResult>> UploadFileAsync(IBrowserFile file, long maxFileSize = 104857600);

    /// <summary>
    /// Uploads a profile picture
    /// POST /api/files/upload/profile-picture
    /// </summary>
    Task<Result<FileUploadResult>> UploadProfilePictureAsync(IBrowserFile file, long maxFileSize = 10485760);

    /// <summary>
    /// Gets file metadata
    /// GET /api/files/{fileId}
    /// </summary>
    Task<Result<FileDto>> GetFileAsync(Guid fileId);

    /// <summary>
    /// Gets download URL for a file
    /// GET /api/files/{fileId}/download
    /// </summary>
    string GetDownloadUrl(Guid fileId);

    /// <summary>
    /// Gets thumbnail URL for an image
    /// GET /api/files/{fileId}/thumbnail
    /// </summary>
    string GetThumbnailUrl(Guid fileId);

    /// <summary>
    /// Gets user's uploaded files
    /// GET /api/files/my-files
    /// </summary>
    Task<Result<List<FileDto>>> GetMyFilesAsync(int pageSize = 50, int skip = 0);

    /// <summary>
    /// Deletes a file
    /// DELETE /api/files/{fileId}
    /// </summary>
    Task<Result> DeleteFileAsync(Guid fileId);
}
