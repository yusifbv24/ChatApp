using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Entities
{
    /// <summary>
    /// Represents a position/role in the organizational hierarchy within a specific department
    /// Examples: Junior IT Specialist, Senior Developer, Team Lead, CEO, etc.
    /// CEO and top-level positions may not belong to any specific department (DepartmentId = null)
    /// </summary>
    public class Position : Entity
    {
        public string Name { get; private set; } = null!;
        public string? Description { get; private set; }

        /// <summary>
        /// Department this position belongs to
        /// NULL for top-level positions like CEO, CTO
        /// </summary>
        public Guid? DepartmentId { get; private set; }
        public Department? Department { get; private set; }

        // Navigation properties
        private readonly List<Employee> _employees = [];
        public IReadOnlyCollection<Employee> Employees => _employees.AsReadOnly();

        // Private constructor for EF Core
        private Position() : base() { }

        public Position(
            string name,
            Guid? departmentId = null,
            string? description = null) : base()
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Position name cannot be empty", nameof(name));

            Name = name;
            DepartmentId = departmentId;
            Description = description;
        }

        public void UpdateDetails(string name, Guid? departmentId, string? description)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Position name cannot be empty", nameof(name));

            Name = name;
            DepartmentId = departmentId;
            Description = description;
            UpdateTimestamp();
        }
    }
}