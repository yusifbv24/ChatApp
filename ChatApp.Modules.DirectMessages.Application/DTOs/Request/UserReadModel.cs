namespace ChatApp.Modules.DirectMessages.Application.DTOs.Request
{
    public record UserReadModel
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Computed property for full name (not mapped to database).
        /// WARNING: Do NOT use in LINQ-to-Entities queries - use FirstName + " " + LastName instead.
        /// </summary>
        public string FullName => $"{FirstName} {LastName}";

        /// <summary>
        /// User role: 0 = User, 1 = Administrator
        /// </summary>
        public int Role { get; set; }

        /// <summary>
        /// Computed property for role name (not mapped to database).
        /// WARNING: Do NOT use in LINQ-to-Entities queries - use Role == 1 ? "Administrator" : "User" instead.
        /// </summary>
        public string RoleName => Role == 1 ? "Administrator" : "User";

        /// <summary>
        /// User's last visit time (UTC)
        /// </summary>
        public DateTime? LastVisit { get; set; }
    }

    /// <summary>
    /// Read model for Identity module's employees table (read-only for cross-module queries).
    /// </summary>
    public record EmployeeReadModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? PositionId { get; set; }
    }

    /// <summary>
    /// Read model for Identity module's positions table (read-only for cross-module queries).
    /// </summary>
    public record PositionReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }
}