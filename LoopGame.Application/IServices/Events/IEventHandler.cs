namespace LoopGame.Application.IServices.Events;

/// <summary>
/// Handler contract for internal application events.
/// </summary>
public interface IEventHandler
{
    void Handle(GameEventDto gameEvent);
}
