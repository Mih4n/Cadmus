namespace Cadmus.Core.Components;

public class ComponentDescriptor : IComponentDescriptor
{
    private const int MaxComponents = sizeof(long) * 8;

    private int count = 0;
    private readonly Dictionary<Type, int> componentIds = [];

    public long Get<T>() => Get(typeof(T));

    public long Get(Type componentType)
    {
        if (componentIds.TryGetValue(componentType, out var id))
            return 1L << id;

        throw new Exception("Component is not registered");
    }

    public IComponentDescriptor Register<T>()
    {
        if (componentIds.ContainsKey(typeof(T)))
            return this;

        if (count >= MaxComponents)
            throw new Exception($"Cannot register more than {MaxComponents} components, the signature is a single long bitmask");

        componentIds.Add(typeof(T), count++);
        return this;
    }
}
