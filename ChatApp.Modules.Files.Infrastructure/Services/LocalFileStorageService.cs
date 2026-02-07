using ChatApp.Modules.Files.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Files.Infrastructure.Services
{
    public class LocalFileStorageService:IFileStorageService
    {
        private readonly string _baseStoragePath;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(
            IConfiguration configuration,
            ILogger<LocalFileStorageService> logger)
        {
            _baseStoragePath = configuration["FileStorage:LocalPath"]
                ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");

            _logger = logger;

            // Ensure base directory exists
            if (!Directory.Exists(_baseStoragePath))
            {
                Directory.CreateDirectory(_baseStoragePath);
                _logger.LogInformation("Created storage directory: {Path}", _baseStoragePath);
            }
        }


        public async Task<string> SaveFileAsync(
            IFormFile file,
            string fileName,
            string directory,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Create subdirectory if it doesnt exist
                var fullDirectoryPath=Path.Combine(_baseStoragePath, directory);
                if (!Directory.Exists(fullDirectoryPath))
                {
                    Directory.CreateDirectory(fullDirectoryPath);
                }

                var fullPath=Path.Combine(fullDirectoryPath, fileName);

                // Save file
                using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream, cancellationToken);

                _logger?.LogInformation("File saved successfully: {Path}", fullPath);
                return fullPath;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving file to storage");
                throw;
            }
        }


        public async Task<Stream> GetFileStreamAsync(
            string storagePath,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(storagePath))
            {
                throw new FileNotFoundException($"File not found: {storagePath}");
            }

            var memoryStream = new MemoryStream();
            using var fileStream = new FileStream(storagePath, FileMode.Open, FileAccess.Read);
            await fileStream.CopyToAsync(memoryStream, cancellationToken);

            memoryStream.Position = 0;
            return memoryStream;
        }


        public Task DeleteFileAsync(string storagePath,CancellationToken cancellationToken = default)
        {
            try
            {
                if (File.Exists(storagePath))
                {
                    File.Delete(storagePath);
                    _logger?.LogInformation("File deleted: {Path}", storagePath);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting file: {Path}", storagePath);
                throw;
            }

            return Task.CompletedTask;
        }



        public Task<bool> FileExistsAsync(string storagePath,CancellationToken cancellationToken = default)
        {
            return Task.FromResult(File.Exists(storagePath));
        }


        public Task<long> GetFileSizeAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(storagePath))
            {
                throw new FileNotFoundException($"File not found: {storagePath}");
            }

            var fileInfo = new FileInfo(storagePath);
            return Task.FromResult(fileInfo.Length);
        }
    }
}