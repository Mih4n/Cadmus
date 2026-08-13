using System.Runtime.InteropServices;

namespace Cadmus.Core.Components;

public interface IComponentArray;

public class ComponentArray<T> : IComponentArray where T : struct
{
    private readonly List<T> items = [];
    public Span<T> AsSpan() => CollectionsMarshal.AsSpan(items);

    public void Add(T item)
    {
        items.Add(item);
    }
}
