using Cadmus.Core.Entities;
using Cadmus.Core.Storage;

namespace Cadmus.Engine;

public sealed class World(
    IEntityStorage storage,
    IEntityFactory factory
)
{
    public EntityBuilder Spawn() => new(storage, factory);

    public EntityBuilder<TEntity> Spawn<TEntity>() where TEntity : class, IEntity
        => new(storage, factory);
}
