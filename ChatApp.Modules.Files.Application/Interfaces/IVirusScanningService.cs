using ChatApp.Modules.Files.Application.DTOs.Responses;

namespace ChatApp.Modules.Files.Application.Interfaces
{
    /// <summary>
    /// Service for scanning files for viruses using ClamAV
    /// </summary>
    public interface IVirusScanningService
    {
        Task<VirusScanResult> ScanFileAsync(string filePath, CancellationToken cancellationToken = default);
    }
}