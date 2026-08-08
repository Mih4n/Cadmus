using Cadmus.Core.Components;
using Cadmus.Core.Entities;
using Cadmus.Core.Game;
using Cadmus.Core.Scenes;
using Cadmus.Engine.Components;

namespace Cadmus.Engine.Scenes;

/// <summary>
/// Base scene. Scenes are resolved from the container, so a derived scene declares the services it
/// needs — typically <see cref="IEntityFactory"/> — as constructor parameters.
/// </summary>
public class Scene : ComposeComponent, IScene
{
    private readonly Dictionary<Guid, IEntity> entities = [];

    protected IEntityFactory EntityFactory { get; }

    public string Name { get; set; }

    public IReadOnlyDictionary<Guid, IEntity> Entities => entities;

    public Scene(IEntityFactory entityFactory)
    {
        EntityFactory = entityFactory;
        Name = GetType().Name;
    }

    public IScene AddEntity(IEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!entities.TryAdd(entity.Id, entity))
        {
            throw new InvalidOperationException($"Entity {entity.Id} already exists in scene '{Name}'.");
        }

        return this;
    }

    /// <summary>Creates an entity through DI and adds it to the scene in one step.</summary>
    public TEntity Spawn<TEntity>(params object[] arguments) where TEntity : IEntity
    {
        var entity = EntityFactory.Create<TEntity>(arguments);
        AddEntity(entity);
        return entity;
    }

    public IEntity Spawn(string name, params IEnumerable<IComponent> components)
    {
        var entity = EntityFactory.CreateFrom(name, components);
        AddEntity(entity);
        return entity;
    }

    public bool RemoveEntity(Guid entityId) => entities.Remove(entityId);

    public IEntity? GetEntity(Guid entityId) => entities.GetValueOrDefault(entityId);

    public virtual Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public virtual Task UpdateAsync(GameTime time, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public virtual Task UnloadAsync(CancellationToken cancellationToken = default)
    {
        entities.Clear();
        return Task.CompletedTask;
    }
}
