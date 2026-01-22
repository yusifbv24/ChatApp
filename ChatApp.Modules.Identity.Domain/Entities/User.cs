using ChatApp.Modules.Identity.Domain.Enums;
using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Entities
{
    /// <summary>
    /// User entity - Represents system users with organizational hierarchy.
    /// </summary>
    public class User : Entity
    {
        // Required fields (NOT NULL)
        public string FirstName { get; private set; } = null!;
        public string LastName { get; private set; } = null!;
        public string Email { get; private set; } = null!; // Unique, used for login
        public string PasswordHash { get; private set; } = null!;
        public bool IsActive { get; private set; } = true;

        // Role (User or Administrator)
        public Role Role { get; private set; } = Role.User;

        // Optional fields (NULLABLE)
        public DateTime? DateOfBirth { get; private set; }
        public string? AvatarUrl { get; private set; }
        public string? WorkPhone { get; private set; }
        public DateTime? HiringDate { get; private set; }
        public DateTime? LastVisit { get; private set; }
        public string? AboutMe { get; private set; }

        // Organizational Structure
        public Guid? PositionId { get; private set; }
        public Position? Position { get; private set; }

        public Guid? DepartmentId { get; private set; }
        public Department? Department { get; private set; }

        public Guid? SupervisorId { get; private set; }
        public User? Supervisor { get; private set; }

        // Denormalized field for subdepartment employees
        // NULL for CEO, Department Heads, Subdepartment Heads
        // SET for subdepartment employees only (points to parent department head)
        public Guid? HeadOfDepartmentId { get; private set; }

        // Navigation properties
        private readonly List<User> _subordinates = [];
        private readonly List<Department> _managedDepartments = [];
        private readonly List<UserPermission> _userPermissions = [];

        public IReadOnlyCollection<User> Subordinates => _subordinates.AsReadOnly();
        public IReadOnlyCollection<Department> ManagedDepartments => _managedDepartments.AsReadOnly();
        public IReadOnlyCollection<UserPermission> UserPermissions => _userPermissions.AsReadOnly();

        // Computed properties
        public string FullName => $"{FirstName} {LastName}";
        public bool IsAdmin => Role == Role.Administrator;
        public bool IsCEO => Position?.Name == "CEO";

        // Private constructor for EF Core
        private User() : base() { }

        /// <summary>
        /// Creates a regular user (employee).
        /// </summary>
        public User(
            string firstName,
            string lastName,
            string email,
            string passwordHash,
            Role role = Role.User,
            string? avatarUrl = null,
            string? aboutMe = null,
            DateTime? dateOfBirth = null,
            string? workPhone = null,
            DateTime? hiringDate = null) : base()
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First name cannot be empty", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name cannot be empty", nameof(lastName));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PasswordHash = passwordHash;
            Role = role;
            AvatarUrl = avatarUrl;
            AboutMe = aboutMe;
            DateOfBirth = dateOfBirth;
            WorkPhone = workPhone;
            HiringDate = hiringDate;
            IsActive = true;
        }

        #region Update Methods

        public void UpdateName(string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First name cannot be empty", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name cannot be empty", nameof(lastName));

            FirstName = firstName;
            LastName = lastName;
            UpdateTimestamp();
        }

        public void UpdateEmail(string newEmail)
        {
            if (string.IsNullOrWhiteSpace(newEmail))
                throw new ArgumentException("Email cannot be empty", nameof(newEmail));

            Email = newEmail;
            UpdateTimestamp();
        }

        public void ChangePassword(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new ArgumentException("Password hash cannot be empty", nameof(newPasswordHash));

            PasswordHash = newPasswordHash;
            UpdateTimestamp();
        }

        public void AssignToPosition(Guid? positionId)
        {
            PositionId = positionId;
            UpdateTimestamp();
        }

        public void UpdateAvatarUrl(string? avatarUrl)
        {
            AvatarUrl = avatarUrl;
            UpdateTimestamp();
        }

        public void UpdateAboutMe(string? aboutMe)
        {
            AboutMe = aboutMe;
            UpdateTimestamp();
        }

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

        public void UpdateHiringDate(DateTime? hiringDate)
        {
            HiringDate = hiringDate;
            UpdateTimestamp();
        }

        public void UpdateLastVisit()
        {
            LastVisit = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void Activate()
        {
            IsActive = true;
            UpdateTimestamp();
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdateTimestamp();
        }

        #endregion

        #region Role and Permission Management

        public void ChangeRole(Role newRole)
        {
            Role = newRole;
            UpdateTimestamp();
        }

        public void PromoteToAdministrator()
        {
            Role = Role.Administrator;
            UpdateTimestamp();
        }

        public void DemoteToUser()
        {
            Role = Role.User;
            UpdateTimestamp();
        }

        public void AssignPermission(UserPermission userPermission)
        {
            if (_userPermissions.Any(up => up.PermissionName == userPermission.PermissionName))
                throw new InvalidOperationException("User already has this permission");

            _userPermissions.Add(userPermission);
            UpdateTimestamp();
        }

        public void RemovePermission(string permissionName)
        {
            var userPermission = _userPermissions.FirstOrDefault(up => up.PermissionName == permissionName);
            if (userPermission != null)
            {
                _userPermissions.Remove(userPermission);
                UpdateTimestamp();
            }
        }

        #endregion

        #region Organizational Structure Methods

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

        public void AssignHeadOfDepartment(Guid headOfDepartmentId)
        {
            if (headOfDepartmentId == Guid.Empty)
                throw new ArgumentException("Head of department ID cannot be empty", nameof(headOfDepartmentId));

            HeadOfDepartmentId = headOfDepartmentId;
            UpdateTimestamp();
        }

        public void RemoveHeadOfDepartment()
        {
            HeadOfDepartmentId = null;
            UpdateTimestamp();
        }

        #endregion
    }
}