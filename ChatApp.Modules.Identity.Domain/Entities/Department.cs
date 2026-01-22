using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Entities
{
    /// <summary>
    /// Department entity - Represents organizational departments and subdepartments.
    /// Supports hierarchical structure (self-referencing).
    /// </summary>
    public class Department : Entity
    {
        public string Name { get; private set; } = null!;

        // Hierarchy (self-referencing for subdepartments)
        public Guid? ParentDepartmentId { get; private set; }
        public Department? ParentDepartment { get; private set; }

        // Department Head
        public Guid? HeadOfDepartmentId { get; private set; }
        public User? HeadOfDepartment { get; private set; }

        // Navigation Properties
        private readonly List<Department> _subdepartments = [];
        private readonly List<User> _employees = [];
        private readonly List<Position> _positions = [];

        public IReadOnlyCollection<Department> Subdepartments => _subdepartments.AsReadOnly();
        public IReadOnlyCollection<User> Employees => _employees.AsReadOnly();
        public IReadOnlyCollection<Position> Positions => _positions.AsReadOnly();

        // Private constructor for EF Core
        private Department() : base() { }

        /// <summary>
        /// Creates a new top-level department (no parent).
        /// </summary>
        public Department(string name, Guid? headOfDepartmentId = null) : base()
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Department name cannot be empty", nameof(name));

            Name = name;
            ParentDepartmentId = null;
            HeadOfDepartmentId = headOfDepartmentId;
        }

        /// <summary>
        /// Creates a new subdepartment (with parent).
        /// </summary>
        public Department(string name, Guid parentDepartmentId, Guid? headOfDepartmentId = null) : base()
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Department name cannot be empty", nameof(name));

            if (parentDepartmentId == Guid.Empty)
                throw new ArgumentException("Parent department ID cannot be empty", nameof(parentDepartmentId));

            Name = name;
            ParentDepartmentId = parentDepartmentId;
            HeadOfDepartmentId = headOfDepartmentId;
        }

        public void UpdateName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Department name cannot be empty", nameof(newName));

            Name = newName;
            UpdateTimestamp();
        }

        public void AssignHead(Guid headOfDepartmentId)
        {
            if (headOfDepartmentId == Guid.Empty)
                throw new ArgumentException("Head of department ID cannot be empty", nameof(headOfDepartmentId));

            HeadOfDepartmentId = headOfDepartmentId;
            UpdateTimestamp();
        }

        public void RemoveHead()
        {
            HeadOfDepartmentId = null;
            UpdateTimestamp();
        }

        public void ChangeParentDepartment(Guid? newParentDepartmentId)
        {
            ParentDepartmentId = newParentDepartmentId;
            UpdateTimestamp();
        }
    }
}
