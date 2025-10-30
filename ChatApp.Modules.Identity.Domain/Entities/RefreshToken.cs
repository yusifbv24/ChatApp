using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Entities
{
    public class RefreshToken:Entity
    {
        public Guid UserId { get; private set; }
        public string? Token { get; private set; }=string.Empty;
        public DateTime ExpiresAtUtc { get; private set; }
        public bool IsRevoked { get; private set; }
        public DateTime? RevokedAtUtc { get; private set; }


        public User User { get; private set; } = null!;

        private RefreshToken():base(){}


        public RefreshToken(Guid userId,string token,DateTime expiresAtUtc) : base()
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be empty", nameof(token));

            UserId = userId;
            Token = token;
            ExpiresAtUtc = expiresAtUtc;
            IsRevoked = false;
        }


        public bool IsExpired()
        {
            return DateTime.UtcNow>=ExpiresAtUtc;
        }


        public bool IsValid()
        {
            return !IsRevoked && !IsExpired();
        }


        public void Revoke()
        {
            IsRevoked=true;
            RevokedAtUtc=DateTime.UtcNow;
            UpdateTimestamp();
        }
    }
}