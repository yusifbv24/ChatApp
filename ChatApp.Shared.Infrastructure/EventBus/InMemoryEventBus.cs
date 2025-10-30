using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatApp.Shared.Infrastructure.EventBus
{
    /// <summary>
    /// In-memory event bus for monolith inter-module communication
    /// </summary>
    public class InMemoryEventBus:IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();
        private readonly ILogger<InMemoryEventBus> _logger;

        public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
        {
            _logger= logger;
        }

        public async Task PublishAsync<TEvent>(TEvent @event,CancellationToken cancellationToken = default) where TEvent:DomainEvent
        {
            var eventType=typeof(TEvent);

            _logger?.LogInformation("Publishing event {EventType} with ID {EventId}", eventType.Name, @event.EventId);

            if(_handlers.TryGetValue(eventType,out var handlers))
            {
                foreach(var handler in handlers)
                {
                    try
                    {
                        await ((Func<TEvent, Task>)handler)(@event);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error handling event {EventType} with ID {EventId}", eventType.Name, @event.EventId);
                    }
                }
            }
            else
            {
                _logger?.LogWarning("No handlers registered for event {EventType}", eventType.Name);
            }
        }


        public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent
        {
            var eventType=typeof(TEvent);

            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType]=new List<Delegate>();
            }

            _handlers[eventType].Add(handler);

            _logger?.LogInformation("Subscribed handler for event {EventType}", eventType.Name);
        }
    }
}