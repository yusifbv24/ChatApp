namespace ChatApp.Client.Models.Identity
{
    public record UserDto
    {
        public Guid Id { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string? AvatarUrl { get; init; }
        public string? Notes { get; init; }
        public Guid CreatedBy { get; init; }
        public bool IsActive { get; init; }
        public bool IsAdmin { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public string DisplayText => !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : Username;
        public string Initials
        {
            get
            {
                var name = DisplayText;
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 0) return "?";
                if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

                // Take first letter of first two parts
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
        }
        public bool HasCustomAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);
    }
}