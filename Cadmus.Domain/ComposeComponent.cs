using System.Diagnostics.CodeAnalysis;

namespace Cadmus.Domain;

public class ComposeComponent : IComposeComponent
{
    Dictionary<Type, List<IComponent>> components = [];

    public void AddComponent(IComponent component)
    {
        var type = component.GetType();
        if (!components.ContainsKey(type))
        {
            components[type] = [];
        }
        components[type].Add(component);
    }

    public void AddComponents(params IEnumerable<IComponent> components)
    {
        foreach (var component in components)
        {
            AddComponent(component);
        }
    }

    public T? GetComponent<T>() where T : IComponent
    {
        if (components.TryGetValue(typeof(T), out var list) && list.Count > 0)
        {
            return (T?)list[0];
        }
        return default;
    }

    public IEnumerable<T> GetComponents<T>() where T : IComponent
    {
        if (components.TryGetValue(typeof(T), out var list))
        {
            return list.Cast<T>();
        }
        return [];
    }

    public bool TryGetComponent<T>([MaybeNullWhen(false)] out T component) where T : IComponent
    {
        if (components.TryGetValue(typeof(T), out var list) && list.Count > 0)
        {
            component = (T)list[0];
            return true;
        }
        component = default;
        return false;
    }

    public bool HasComponent<T>() where T : IComponent
    {
        return components.ContainsKey(typeof(T)) && components[typeof(T)].Count > 0;
    }

    public int GetComponentCount<T>() where T : IComponent
    {
        if (components.TryGetValue(typeof(T), out var list))
        {
            return list.Count;
        }
        return 0;
    }

    public void RemoveComponent<T>(T component) where T : IComponent
    {
        if (components.TryGetValue(typeof(T), out var list))
        {
            list.Remove(component);
            if (list.Count == 0)
            {
                components.Remove(typeof(T));
            }
        }
    }

    public void RemoveAllComponents<T>() where T : IComponent
    {
        components.Remove(typeof(T));
    }
}
