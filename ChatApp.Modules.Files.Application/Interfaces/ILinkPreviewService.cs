using ChatApp.Modules.Files.Application.DTOs.Responses;

namespace ChatApp.Modules.Files.Application.Interfaces;

public interface ILinkPreviewService
{
    Task<LinkPreviewDto?> GetPreviewAsync(string url, CancellationToken cancellationToken = default);
}