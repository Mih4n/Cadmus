using Cadmus.Core.Components;

namespace Cadmus.Core.Storage;

public class EntityBank(long descriptor)
{
    public readonly long Descriptor = descriptor;

    private Dictionary<Type, IComponentArray> bank;

    public Span<T> GetComponents<T>() where T : struct
    {
        var array = bank.Get
    }
}