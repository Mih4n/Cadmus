using Cadmus.Domain.Contracts;

namespace Cadmus.Domain;

public interface IComposeComponent : IComponent, IReadOnlyComposeComponent
{
    void AddComponent(IComponent component);
    void AddComponents(params IEnumerable<IComponent> components);
    void RemoveComponent<T>(T component) where T : IComponent;
    void RemoveAllComponents<T>() where T : IComponent;
}
