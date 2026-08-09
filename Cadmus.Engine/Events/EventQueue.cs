using Cadmus.Core.Events;
using Cadmus.Core.Game;
using Cadmus.Core.Systems;

namespace Cadmus.Engine.Events;

/// <inheritdoc cref="IEventQueue"/>
public sealed class EventQueue : IEventQueue, ISystem
{
    private readonly Dictionary<Type, IEventList> lists = [];

    /// <summary>
    /// Empties the queue before anything else runs, so a frame never observes the previous frame's
    /// events and no system has to remember to clean up after itself.
    /// </summary>
    public int Order => int.MinValue;

    public ValueTask UpdateAsync(GameTime time, CancellationToken cancellationToken = default)
    {
        Clear();
        return ValueTask.CompletedTask;
    }

    public void Publish<TEvent>(TEvent @event) where TEvent : notnull => GetList<TEvent>().Add(@event);

    public IReadOnlyList<TEvent> Read<TEvent>() where TEvent : notnull =>
        lists.TryGetValue(typeof(TEvent), out var list) ? ((EventList<TEvent>)list).Items : [];

    public bool Has<TEvent>() where TEvent : notnull => Read<TEvent>().Count > 0;

    public void Clear()
    {
        // The per-type lists are kept and only emptied, so a steady-state frame allocates nothing.
        foreach (var list in lists.Values)
        {
            list.Clear();
        }
    }

    private EventList<TEvent> GetList<TEvent>() where TEvent : notnull
    {
        if (lists.TryGetValue(typeof(TEvent), out var existing))
        {
            return (EventList<TEvent>)existing;
        }

        var created = new EventList<TEvent>();
        lists[typeof(TEvent)] = created;

        return created;
    }

    private interface IEventList
    {
        void Clear();
    }

    private sealed class EventList<TEvent> : IEventList
    {
        public List<TEvent> Items { get; } = [];

        public void Add(TEvent @event) => Items.Add(@event);

        public void Clear() => Items.Clear();
    }
}
