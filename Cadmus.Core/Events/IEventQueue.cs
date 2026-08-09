namespace Cadmus.Core.Events;

/// <summary>
/// Frame-scoped message bus between systems. A system publishes a fact, later systems read it —
/// which keeps them decoupled: the mover does not know that scoring or audio exist.
/// </summary>
/// <remarks>
/// Events live for exactly one frame: the queue is emptied at the start of every frame, before any
/// system runs. Visibility therefore follows <c>ISystem.Order</c> — a consumer must be ordered after
/// its publisher to see the event in the same frame.
/// </remarks>
public interface IEventQueue
{
    void Publish<TEvent>(TEvent @event) where TEvent : notnull;

    /// <summary>Everything of this type published so far this frame, in publication order.</summary>
    IReadOnlyList<TEvent> Read<TEvent>() where TEvent : notnull;

    bool Has<TEvent>() where TEvent : notnull;

    void Clear();
}
