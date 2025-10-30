using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.ValueObjects
{
    public class Password:ValueObject
    {
        public string Value { get; }

        private const int MinimumLength = 0;

        private Password(string value)
        {
            Value= value;
        }


        public static Password Create(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty");

            if (password.Length < MinimumLength)
                throw new ArgumentException($"Password must be at least {MinimumLength} characters long");

            if (!HasUpperCase(password))
                throw new ArgumentException("Password must contain at least one uppercase letter");

            if (!HasLowerCase(password))
                throw new ArgumentException("Password must contain at least one lowercase letter");

            if (!HasDigit(password))
                throw new ArgumentException("Password must contain at least one digit");

            return new Password(password);
        }


        private static bool HasUpperCase(string password) => password.Any(char.IsUpper);
        private static bool HasLowerCase(string password) => password.Any(char.IsLower);
        private static bool HasDigit(string password)=>password.Any(char.IsDigit);
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
        public override string ToString() => "***";
    }
}