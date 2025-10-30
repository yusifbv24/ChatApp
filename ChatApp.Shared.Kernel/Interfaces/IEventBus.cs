using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Shared.Kernel.Interfaces
{
    public interface IEventBus
    {
        Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : DomainEvent;
        void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent;
    }
}