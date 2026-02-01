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

        // Company relationship
        public Guid CompanyId { get; private set; }
        public Company? Company { get; private set; }

        // Hierarchy (self-referencing for subdepartments)
        public Guid? ParentDepartmentId { get; private set; }
        public Department? ParentDepartment { get; private set; }

        // Department Head
        public Guid? HeadOfDepartmentId { get; private set; }
        public User? HeadOfDepartment { get; private set; }

        // Navigation Properties
        private readonly List<Department> _subdepartments = [];
        private readonly List<Employee> _employees = [];
        private readonly List<Position> _positions = [];

        public IReadOnlyCollection<Department> Subdepartments => _subdepartments.AsReadOnly();
        public IReadOnlyCollection<Employee> Employees => _employees.AsReadOnly();
        public IReadOnlyCollection<Position> Positions => _positions.AsReadOnly();

        // Private constructor for EF Core
        private Department() : base() { }

        /// <summary>
        /// Creates a new top-level department (no parent).
        /// </summary>
        public Department(string name, Guid companyId, Guid? headOfDepartmentId = null) : base()
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Department name cannot be empty", nameof(name));

            if (companyId == Guid.Empty)
                throw new ArgumentException("Company ID cannot be empty", nameof(companyId));

            Name = name;
            CompanyId = companyId;
            ParentDepartmentId = null;
            HeadOfDepartmentId = headOfDepartmentId;
        }

        /// <summary>
        /// Creates a new subdepartment (with parent).
        /// </summary>
        public Department(string name, Guid companyId, Guid parentDepartmentId, Guid? headOfDepartmentId = null) : base()
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Department name cannot be empty", nameof(name));

            if (companyId == Guid.Empty)
                throw new ArgumentException("Company ID cannot be empty", nameof(companyId));

            if (parentDepartmentId == Guid.Empty)
                throw new ArgumentException("Parent department ID cannot be empty", nameof(parentDepartmentId));

            Name = name;
            CompanyId = companyId;
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
