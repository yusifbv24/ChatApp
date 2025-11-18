namespace ChatApp.Shared.Kernel.Common
{
    public abstract record DomainEvent
    {
        public Guid EventId { get; }
        public DateTime occurredAtUtc { get; }

        protected DomainEvent()
        {
            EventId= Guid.NewGuid();
            occurredAtUtc= DateTime.UtcNow;
        }
    }
}