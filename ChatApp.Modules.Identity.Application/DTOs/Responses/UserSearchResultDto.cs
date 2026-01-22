namespace ChatApp.Modules.Identity.Application.DTOs.Responses
{
    /// <summary>
    /// Minimal user information for search results
    /// Used when searching for users to start conversations
    /// </summary>
    public record UserSearchResultDto(
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        string? AvatarUrl,
        string? Position)
    {
        public string FullName => $"{FirstName} {LastName}";
    };
}