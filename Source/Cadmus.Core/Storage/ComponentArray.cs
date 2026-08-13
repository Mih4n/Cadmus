using System.Runtime.InteropServices;

namespace Cadmus.Core.Storage;

public sealed class ComponentArray<T> : IComponentArray where T : struct
{
    private readonly List<T> items = [];

    public int Count => items.Count;

    public Span<T> AsSpan() => CollectionsMarshal.AsSpan(items);

    public int Add(T value)
    {
        items.Add(value);
        return items.Count - 1;
    }

    public void SwapRemoveAt(int row)
    {
        items[row] = items[^1];
        items.RemoveAt(items.Count - 1);
    }

    public void CopyRowTo(int row, IComponentArray destination)
    {
        ((ComponentArray<T>)destination).Add(items[row]);
    }
}
