using Cadmus.Core.Components;
using Cadmus.Core.Entities;

namespace Cadmus.Core.Storage;

public sealed class EntityStorage : IEntityStorage
{
    private readonly IComponentDescriptor descriptor;
    private readonly Dictionary<long, Archetype> archetypes = [];
    private readonly Dictionary<Guid, (Archetype Archetype, int Row)> locations = [];

    public EntityStorage(IComponentDescriptor descriptor)
    {
        this.descriptor = descriptor;
        archetypes[0] = new Archetype(0, []);
    }

    public void Add(IEntity entity)
    {
        var empty = archetypes[0];
        int row = empty.AddEntity(entity);
        locations[entity.Id] = (empty, row);
    }

    public void Remove(IEntity entity)
    {
        var (archetype, row) = locations[entity.Id];
        var moved = archetype.SwapRemove(row);
        if (moved is not null)
            locations[moved.Id] = (archetype, row);

        locations.Remove(entity.Id);
    }

    public void AddComponent<T>(IEntity entity, T component) where T : struct
        => Move<T>(entity, add: true, component);

    public void RemoveComponent<T>(IEntity entity, T component) where T : struct
        => Move<T>(entity, add: false, component);

    public void Query<T>(Action<Span<T>> action) where T : struct
    {
        foreach (var archetype in archetypes.Values)
        {
            if (archetype.Count == 0 || !archetype.HasComponent<T>())
                continue;

            action(archetype.GetColumn<T>().AsSpan());
        }
    }

    public void Query<T1, T2>(Action<Span<T1>, Span<T2>> action) where T1 : struct where T2 : struct
    {
        foreach (var archetype in archetypes.Values)
        {
            if (archetype.Count == 0 || !archetype.HasComponent<T1>() || !archetype.HasComponent<T2>())
                continue;

            action(archetype.GetColumn<T1>().AsSpan(), archetype.GetColumn<T2>().AsSpan());
        }
    }

    private void Move<T>(IEntity entity, bool add, T value) where T : struct
    {
        descriptor.Register<T>();

        var (from, row) = locations[entity.Id];
        long bit = descriptor.Get<T>();
        long targetSignature = add ? from.Signature | bit : from.Signature & ~bit;

        if (targetSignature == from.Signature)
            return;

        if (!archetypes.TryGetValue(targetSignature, out var to))
        {
            var targetTypes = add
                ? from.ComponentTypes.Append(typeof(T))
                : from.ComponentTypes.Where(type => type != typeof(T));

            to = new Archetype(targetSignature, targetTypes);
            archetypes[targetSignature] = to;
        }

        foreach (var (type, column) in from.Columns)
        {
            if (!add && type == typeof(T))
                continue;

            column.CopyRowTo(row, to.GetColumn(type));
        }

        if (add)
            to.GetColumn<T>().Add(value);

        int newRow = to.AddEntity(entity);

        var moved = from.SwapRemove(row);
        if (moved is not null)
            locations[moved.Id] = (from, row);

        locations[entity.Id] = (to, newRow);
    }
}
