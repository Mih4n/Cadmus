using Cadmus.Core.Components;

namespace Cadmus.Core.Entities;

/// <summary>
/// Creates entities through the DI container: constructor parameters that the container knows about
/// are injected, the rest are taken from <paramref name="arguments"/>. This is how an entity gets a
/// service — never by reaching into a global context.
/// </summary>
public interface IEntityFactory
{
    TEntity Create<TEntity>(params object[] arguments) where TEntity : IEntity;

    IEntity Create(Type entityType, params object[] arguments);

    /// <summary>Creates a plain entity that only carries the given components.</summary>
    IEntity CreateFrom(string name, params IEnumerable<IComponent> components);
}
