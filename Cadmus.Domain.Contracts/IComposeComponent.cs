using Cadmus.Domain.Contracts;

namespace Cadmus.Domain;

public interface IComposeComponent : IComponent, IReadOnlyComposeComponent
{
    void SetComponent(IComponent component);
    void SetComponents(params IEnumerable<IComponent> components);
    void RemoveComponent<T>() where T : IComponent;
}
