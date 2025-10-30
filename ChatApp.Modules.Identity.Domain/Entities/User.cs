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

        // Navigation properties
        private readonly List<UserRole> _userRoles = new();

        public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

        private User() :base() { }

        public User(string username,string email,string passwordHash,bool isAdmin = false):base()
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException("Usename cannot be empty", nameof(username));

            if(string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty",nameof(email));

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

            Username = username;
            Email = email;
            PasswordHash = passwordHash;
            IsActive = true;
            IsAdmin = isAdmin;
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