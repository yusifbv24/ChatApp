namespace ChatApp.Modules.Channels.Domain.ValueObjects
{
    /// <summary>
    /// Value object for channel name with validation.
    /// Immutable and defined by its value, not identity
    /// </summary>
    public class ChannelName : IEquatable<ChannelName>
    {
        public string Value { get; }

        private ChannelName(string value)
        {
            Value = value;
        }

        public static ChannelName Create(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Channel name cannot be empty", nameof(value));

            if (value.Length < 2)
                throw new ArgumentException("Channel name must be at least 2 characters", nameof(value));

            if (value.Length > 100)
                throw new ArgumentException("Channel name cannot exceed 100 characters", nameof(value));

            if (value.ToLower().Trim().Contains("!@#$%^&*()_+=-`';\\,][}{|:"))
                throw new ArgumentException("Channel name cannot must not any special characters", nameof(value));

            return new ChannelName(value.Trim());
        }

        public bool Equals(ChannelName? other)
        {
            if (other is null) return false;
            return Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => Equals(obj as ChannelName);
        public override int GetHashCode() => Value.GetHashCode(StringComparison.OrdinalIgnoreCase);
        public override string ToString() => Value;
        public static implicit operator string(ChannelName name) => name.Value;
    }
}