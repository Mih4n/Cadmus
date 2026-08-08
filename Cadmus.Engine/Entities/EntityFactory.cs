using Cadmus.Core.Components;
using Cadmus.Core.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Cadmus.Engine.Entities;

/// <inheritdoc cref="IEntityFactory"/>
public sealed class EntityFactory(IServiceProvider services) : IEntityFactory
{
    public TEntity Create<TEntity>(params object[] arguments) where TEntity : IEntity =>
        (TEntity)Create(typeof(TEntity), arguments);

    public IEntity Create(Type entityType, params object[] arguments)
    {
        if (!typeof(IEntity).IsAssignableFrom(entityType))
        {
            throw new ArgumentException($"{entityType.Name} does not implement {nameof(IEntity)}.", nameof(entityType));
        }

        // A registered entity is resolved as-is; anything else is still constructed through the
        // container so its constructor dependencies get injected.
        if (arguments.Length == 0 && services.GetService(entityType) is IEntity registered)
        {
            return registered;
        }

        return (IEntity)ActivatorUtilities.CreateInstance(services, entityType, arguments);
    }

    public IEntity CreateFrom(string name, params IEnumerable<IComponent> components) =>
        new Entity(name, components);
}
