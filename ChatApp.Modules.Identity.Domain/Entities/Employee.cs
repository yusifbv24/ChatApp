using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Entities
{
    /// <summary>
    /// Employee entity - Contains organizational and sensitive employee data
    /// 1:1 relationship with User (every User is an Employee)
    /// Sensitive fields (DateOfBirth, WorkPhone, AboutMe) are encrypted at database level
    /// </summary>
    public class Employee : Entity
    {
        // Foreign Key to User (1:1 mandatory)
        public Guid UserId { get; private set; }
        public User User { get; private set; } = null!;

        // Sensitive Personal Information (ENCRYPTED at DB level)
        public DateTime? DateOfBirth { get; private set; }
        public string? WorkPhone { get; private set; }
        public string? AboutMe { get; private set; }

        // Organizational Structure
        public Guid? PositionId { get; private set; }
        public Position? Position { get; private set; }

        public Guid? DepartmentId { get; private set; }
        public Department? Department { get; private set; }

        public Guid? SupervisorId { get; private set; }
        public Employee? Supervisor { get; private set; }

        // Denormalized field for subdepartment employees
        // NULL for CEO, Department Heads, Subdepartment Heads
        // SET for subdepartment employees only (points to parent department head)
        public Guid? HeadOfDepartmentId { get; private set; }

        // Employment Information
        public DateTime? HiringDate { get; private set; }

        // Navigation properties
        private readonly List<Employee> _subordinates = [];
        public IReadOnlyCollection<Employee> Subordinates => _subordinates.AsReadOnly();

        // Private constructor for EF Core
        private Employee() : base() { }

        /// <summary>
        /// Creates an employee record
        /// </summary>
        public Employee(
            Guid userId,
            DateTime? dateOfBirth = null,
            string? workPhone = null,
            string? aboutMe = null,
            DateTime? hiringDate = null) : base()
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            UserId = userId;
            DateOfBirth = dateOfBirth;
            WorkPhone = workPhone;
            AboutMe = aboutMe;
            HiringDate = hiringDate;
        }

        #region Update Methods

        public void UpdateDateOfBirth(DateTime? dateOfBirth)
        {
            DateOfBirth = dateOfBirth;
            UpdateTimestamp();
        }

        public void UpdateWorkPhone(string? workPhone)
        {
            WorkPhone = workPhone;
            UpdateTimestamp();
        }

        public void UpdateAboutMe(string? aboutMe)
        {
            AboutMe = aboutMe;
            UpdateTimestamp();
        }

        public void UpdateHiringDate(DateTime? hiringDate)
        {
            HiringDate = hiringDate;
            UpdateTimestamp();
        }

        public void AssignToPosition(Guid? positionId)
        {
            PositionId = positionId;
            UpdateTimestamp();
        }

        public void AssignToDepartment(Guid departmentId, Guid? supervisorId = null, Guid? headOfDepartmentId = null)
        {
            if (departmentId == Guid.Empty)
                throw new ArgumentException("Department ID cannot be empty", nameof(departmentId));

            DepartmentId = departmentId;
            SupervisorId = supervisorId;
            HeadOfDepartmentId = headOfDepartmentId;
            UpdateTimestamp();
        }

        public void RemoveFromDepartment()
        {
            DepartmentId = null;
            SupervisorId = null;
            HeadOfDepartmentId = null;
            UpdateTimestamp();
        }

        public void AssignSupervisor(Guid supervisorId)
        {
            if (supervisorId == Guid.Empty)
                throw new ArgumentException("Supervisor ID cannot be empty", nameof(supervisorId));

            SupervisorId = supervisorId;
            UpdateTimestamp();
        }

        public void RemoveSupervisor()
        {
            SupervisorId = null;
            UpdateTimestamp();
        }

        #endregion
    }
}