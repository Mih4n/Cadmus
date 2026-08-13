using Cadmus.Core.Storage;

namespace Cadmus.Core.Entities;

public class EntityBuilder(
    IEntityStorage storage,
    IEntityFactory factory
) : EntityBuilder<Entity>(storage, factory);

public class EntityBuilder<TEntity> where TEntity : class, IEntity
{
    private TEntity entity;
    private IEntityStorage storage;

    public EntityBuilder(
        IEntityStorage storage,
        IEntityFactory factory
    )
    {
        this.storage = storage;

        entity = factory.Create<TEntity>();
        storage.Add(entity);
    }

    public EntityBuilder<TEntity> WithName(string name)
    {
        entity.Name = name;
        return this;
    }

    public EntityBuilder<TEntity> AddComponent<T>(T component) where T : struct
    {
        storage.AddComponent(entity, component);
        return this;
    }

    public TEntity Build()
    {
        return entity;
    }
}
