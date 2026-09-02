namespace LoopGame.Application.IServices.Events;

/// <summary>
/// Publishes in-process application events without coupling to any specific consumer.
/// </summary>
public interface IEventPublisher
{
    void Publish(GameEventDto gameEvent);
}
