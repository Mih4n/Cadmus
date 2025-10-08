using System.Diagnostics.CodeAnalysis;

namespace Cadmus.Domain;

public interface IComposeComponent : IComponent
{
    T? GetComponent<T>() where T : IComponent;
    bool TryGetComponent<T>([MaybeNullWhen(false)] out T component) where T : IComponent;
    bool HasComponent<T>() where T : IComponent;    
    void AddComponent(IComponent component);
    void AddComponents(params IEnumerable<IComponent> components);
    void RemoveComponent<T>() where T : IComponent;
}
