using ChatApp.Modules.Files.Application.DTOs.Responses;
using ChatApp.Modules.Files.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ChatApp.Modules.Files.Infrastructure.Services
{
    public class ClamAVScanningService : IVirusScanningService
    {
        private readonly string _clamScanPath;
        private readonly ILogger<ClamAVScanningService> _logger;

        public ClamAVScanningService(
            IConfiguration configuration,
            ILogger<ClamAVScanningService> logger)
        {
            // Path to clamscan.exe (install ClamAV on server)
            _clamScanPath = configuration["VirusScanning:ClaimScanPath"]
                ?? @"C:\Program Files\ClamAV\clamscan.exe";
            _logger = logger;
        }

        public async Task<VirusScanResult> ScanFileAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(_clamScanPath))
                {
                    _logger?.LogWarning("ClamAV not found at {Path}. Skipping virus scan.", _clamScanPath);
                    return new  VirusScanResult
                    {
                        IsClean = true,
                        Details = "Virus scanning not available - ClamAV not installed"
                    };
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = _clamScanPath,
                    Arguments = $"--no-summary \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process=new Process { StartInfo= startInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync(cancellationToken);

                // Exit code 0=clean, 1=virus found
                if (process.ExitCode == 0)
                {
                    _logger?.LogInformation("File {FilePath} is clean", filePath);
                    return new VirusScanResult
                    {
                        IsClean = true,
                        Details = "File is clean"
                    };
                }
                else if(process.ExitCode == 1)
                {
                    _logger.LogWarning("VIRUS DETECTED in file {FilePath}: {Output}", filePath, output);

                    // Extract threat name from output
                    var threatName = ExtractThreatName(output);

                    return new VirusScanResult
                    {
                        IsClean = false,
                        ThreatName = threatName,
                        Details = output
                    };
                }
                else
                {
                    _logger.LogError("ClamAV scan error for {FilePath}: {Error}", filePath, error);
                    throw new Exception($"Virus scan failed: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning file {FilePath} for viruses", filePath);
                throw;
            }
        }
        private string ExtractThreatName(string clamOutput)
        {
            // ClamAV output format: "filename: ThreatName FOUND"
            var lines = clamOutput.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("FOUND"))
                {
                    var parts = line.Split(':');
                    if (parts.Length > 1)
                    {
                        return parts[1].Replace("FOUND", "").Trim();
                    }
                }
            }
            return "Unknown threat";
        }
    }
}