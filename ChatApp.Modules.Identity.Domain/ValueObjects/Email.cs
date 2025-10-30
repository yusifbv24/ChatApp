using ChatApp.Shared.Kernel.Common;
using System.Text.RegularExpressions;

namespace ChatApp.Modules.Identity.Domain.ValueObjects
{
    public class Email:ValueObject
    {
        public string Value { get; }
        private Email(string value)
        {
            Value= value;
        }

        public static Email Create(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException("Email cannot be empty", nameof(email));

            email = email.Trim().ToLowerInvariant();

            if (!IsValidEmail(email))
                throw new ArgumentException("Invalid email format");

            return new Email(email);
        }


        public static bool IsValidEmail(string email)
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return emailRegex.IsMatch(email);
        }


        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }


        public override string ToString()=> Value;
    }
}