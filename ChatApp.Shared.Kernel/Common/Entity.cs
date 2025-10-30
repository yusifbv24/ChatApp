namespace ChatApp.Shared.Kernel.Common
{
    public abstract class Entity
    {
        public Guid Id { get; set;  }
        public DateTime CreatedAtUtc { get; protected set; }
        public DateTime UpdatedAtUtc { get; protected set; }

        protected Entity()
        {
            Id= Guid.NewGuid();
            CreatedAtUtc= DateTime.UtcNow;
            UpdatedAtUtc= DateTime.UtcNow;
        }

        protected Entity(Guid id)
        {
            Id = id;
            CreatedAtUtc= DateTime.UtcNow;
            UpdatedAtUtc= DateTime.UtcNow;
        }
        public void UpdateTimestamp()
        {
            UpdatedAtUtc= DateTime.UtcNow;
        }

        public override bool Equals(object? obj)
        {
            if(obj is not Entity other) 
                return false;

            if(ReferenceEquals(this,other))
                return true;

            if(GetType()!=other.GetType())
                return false;

            return Id == other.Id;
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(Entity? a, Entity? b)
        {
            if (a is null && b is null)
                return true;

            if (a is null || b is null)
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(Entity? a, Entity? b)
        {
            return !(a == b);
        }
    }
}