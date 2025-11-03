namespace ChatApp.Shared.Kernel.Common
{
    public abstract record DomainEvent
    {
        public Guid EventId { get; }
        public DateTime OccuredAtUtc { get; }

        protected DomainEvent()
        {
            EventId= Guid.NewGuid();
            OccuredAtUtc= DateTime.UtcNow;
        }
    }
}