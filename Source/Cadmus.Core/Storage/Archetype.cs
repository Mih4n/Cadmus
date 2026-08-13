using Cadmus.Core.Entities;

namespace Cadmus.Core.Storage;

public sealed class Archetype(long signature, IEnumerable<Type> componentTypes)
{
    public int Count => entities.Count;
    public long Signature { get; } = signature;
    public IReadOnlyCollection<Type> ComponentTypes => columns.Keys;
    public IEnumerable<KeyValuePair<Type, IComponentArray>> Columns => columns;

    private readonly Dictionary<Type, IComponentArray> columns = componentTypes.ToDictionary(type => type, CreateColumn);
    private readonly List<IEntity> entities = [];

    private static IComponentArray CreateColumn(Type componentType)
    {
        var columnType = typeof(ComponentArray<>).MakeGenericType(componentType);
        return (IComponentArray)Activator.CreateInstance(columnType)!;
    }

    public bool HasComponent<T>() where T : struct => columns.ContainsKey(typeof(T));

    public ComponentArray<T> GetColumn<T>() where T : struct => (ComponentArray<T>)columns[typeof(T)];

    public IComponentArray GetColumn(Type componentType) => columns[componentType];

    public int AddEntity(IEntity entity)
    {
        entities.Add(entity);
        return entities.Count - 1;
    }

    public IEntity? SwapRemove(int row)
    {
        foreach (var column in columns.Values)
            column.SwapRemoveAt(row);

        int last = entities.Count - 1;
        if (row == last)
        {
            entities.RemoveAt(last);
            return null;
        }

        var moved = entities[last];
        entities[row] = moved;
        entities.RemoveAt(last);
        return moved;
    }
}
