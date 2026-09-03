using Microsoft.Extensions.Logging;

namespace LoopGame.Application.Services.Events;

/// <summary>
/// Dispatches internal application events to all registered <see cref="IEventHandler"/> instances.
/// Isolates exceptions per handler so a failure in one subscriber does not prevent execution of others.
/// </summary>
public class InProcessEventPublisher(
    IEnumerable<IEventHandler> _handlers,
    ILogger<InProcessEventPublisher> _logger) : IEventPublisher
{
    public void Publish(GameEventDto gameEvent)
    {
        foreach (var handler in _handlers)
        {
            try
            {
                handler.Handle(gameEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Event handler {HandlerName} failed while processing event {EventType} for player {PlayerId}",
                    handler.GetType().Name, gameEvent.EventType, gameEvent.PlayerId);
            }
        }
    }
}
