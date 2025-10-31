using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Entities
{
    public class User:Entity
    {
        public string Username { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public string PasswordHash { get; private set; } = null!;
        public bool IsActive { get; private set;  }
        public bool IsAdmin { get; private set;  }
        public string DisplayName { get; private set; } = null!;
        public Guid CreatedBy { get;private set;  }
        public string? AvatarUrl { get; private set; } = string.Empty;
        public string? Notes { get; private set; } = string.Empty;

        // Navigation properties
        private readonly List<UserRole> _userRoles = new();

        public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

        private User() :base() { }

        public User(
            string username,
            string email,
            string passwordHash,
            string displayName,
            Guid createdBy,
            string? avatarUrl,
            string? notes,
            bool isAdmin = false) :base()
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException("Usename cannot be empty", nameof(username));

            if(string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty",nameof(email));

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

            if(string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name cannot be empty",nameof(displayName));

            if (createdBy == Guid.Empty)
                throw new ArgumentException("CreatedBy cannot be null", nameof(createdBy));

            Username = username;
            Email = email;
            PasswordHash = passwordHash;
            DisplayName = displayName;
            CreatedBy = createdBy;
            AvatarUrl = avatarUrl ?? string.Empty;
            Notes= notes ?? string.Empty;
            IsActive = true;
            IsAdmin = isAdmin;
        }


        public void ChangeDisplayName(string newDisplayName)
        {
            if (string.IsNullOrWhiteSpace(newDisplayName))
                throw new ArgumentException("Display name cannot be empty", nameof(newDisplayName));

            DisplayName= newDisplayName;
            UpdateTimestamp();
        }


        public void ChangeAvatar(string? newAvatarUrl)
        {
            AvatarUrl= newAvatarUrl ?? string.Empty;
            UpdateTimestamp();
        }


        public void UpdateNotes(string? notes)
        {
            Notes=notes ?? string.Empty;
            UpdateTimestamp();
        }


        public void ChangePassword(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new ArgumentException("Password hash cannot be empty", nameof(newPasswordHash));

            PasswordHash = newPasswordHash;
            UpdateTimestamp();
        }


        public void Deactivate()
        {
            IsActive = false;
            UpdateTimestamp();
        }


        public void Activate()
        {
            IsActive = true;
            UpdateTimestamp();
        }


        public void UpdateEmail(string newEmail)
        {
            if (string.IsNullOrWhiteSpace(newEmail))
                throw new ArgumentException("Email cannot be empty", nameof(newEmail));

            Email = newEmail;
            UpdateTimestamp();
        }



        public void UpdateUsername(string newUsername)
        {
            if (string.IsNullOrWhiteSpace(newUsername))
                throw new ArgumentException("Username cannot be empty", nameof(newUsername));

            Username = newUsername;
            UpdateTimestamp();
        }


        public void MakeAdmin()
        {
            IsAdmin = true;
            UpdateTimestamp();
        }



        public void RevokeAdmin()
        {
            IsAdmin = false;
            UpdateTimestamp();
        }



        public void AssignRole(UserRole userRole)
        {
            if (_userRoles.Any(ur => ur.RoleId == userRole.RoleId))
                throw new InvalidOperationException("User already has this role");

            _userRoles.Add(userRole);
            UpdateTimestamp();
        }



        public void RemoveRole(Guid roleId)
        {
            var userRole = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
            if (userRole != null)
            {
                _userRoles.Remove(userRole);
                UpdateTimestamp();
            }
        }
    }
}