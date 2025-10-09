using System.Diagnostics.CodeAnalysis;

namespace Cadmus.Domain;

public class ComposeComponent : IComposeComponent
{
    Dictionary<Type, IComponent> components = [];

    public void SetComponent(IComponent component)
    {
        components[component.GetType()] = component;
    }

    public void SetComponents(params IEnumerable<IComponent> components)
    {
        foreach (var component in components) 
        {
            SetComponent(component);
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
