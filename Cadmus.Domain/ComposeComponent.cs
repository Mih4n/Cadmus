
using System.Diagnostics.CodeAnalysis;

namespace Cadmus.Domain;

public class ComposeComponent : IComposeComponent
{
    Dictionary<Type, IComponent> components = [];

    public void AddComponent(IComponent component)
    {
        components.Add(component.GetType(), component);
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
        return (T?)components.GetValueOrDefault(typeof(T));
    }

    public bool TryGetComponent<T>([MaybeNullWhen(false)] out T component) where T : IComponent
    {
        components.TryGetValue(typeof(T), out var result);
        component = (T?)result;
        return result == default ? false : true;
    }

    public bool HasComponent<T>() where T : IComponent
    {
        return components.ContainsKey(typeof(T));
    }

    public void RemoveComponent<T>() where T : IComponent
    {
        components.Remove(typeof(T));
    }
}
