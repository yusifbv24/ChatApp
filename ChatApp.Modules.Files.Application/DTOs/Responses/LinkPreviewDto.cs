namespace ChatApp.Modules.Files.Application.DTOs.Responses;

public record LinkPreviewDto(
    string Url,
    string? Title,
    string? Description,
    string? ImageUrl,
    string? Domain
);