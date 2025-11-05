using Microsoft.AspNetCore.Http;

namespace ChatApp.Modules.Files.Application.Interfaces
{
    /// <summary>
    /// Service for storing and retrieving files
    /// Implementation can be local file system, Azure Blob, AWS S3, etc.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves a file to storage
        /// </summary>
        /// <returns>Storage path where file was saved</returns>
        Task<string> SaveFileAsync(IFormFile file,string fileName,string directory,CancellationToken cancellationToken=default);


        /// <summary>
        /// Retrieves a file from storage as a stream
        /// </summary>
        Task<Stream> GetFileStreamAsync(string storagePath,CancellationToken cancellationToken=default);



        /// <summary>
        /// Deletes a file from storage
        /// </summary>
        Task DeleteFileAsync(string storagePath,CancellationToken cancellationToken=default);



        /// <summary>
        /// Checks if file exists in storage
        /// </summary>
        Task<bool> FileExistsAsync(string storagePath,CancellationToken cancellationToken = default);


        /// <summary>
        /// Gets file size in bytes
        /// </summary>
        Task<long> GetFileSizeAsync(string storagePath, CancellationToken cancellationToken = default);
    }
}