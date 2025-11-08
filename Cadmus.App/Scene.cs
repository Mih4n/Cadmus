using Cadmus.Domain.Components;
using Cadmus.Domain.Contracts.Components;
using Cadmus.Domain.Contracts.Entities;

namespace Cadmus.App;

public class Scene : ComposeComponent, IScene
{
    private readonly Dictionary<Guid, IEntity> entities = [];
    
    public IReadOnlyDictionary<Guid, IEntity> Entities => entities;

    public IScene AddEntity(IEntity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        if (entities.ContainsKey(entity.Id))
        {
            throw new InvalidOperationException($"Entity with ID {entity.Id} already exists in the scene.");
        }

        entities.Add(entity.Id, entity);

        return this;
    }

    public IScene RemoveEntity(Guid entityId)
    {
        entities.Remove(entityId);
        return this;
    }

    public IEntity? GetEntity(Guid entityId)
    {
        entities.TryGetValue(entityId, out var entity);
        return entity;
    }
    
    public Task LoadAsync()
    {
        return Task.CompletedTask;
    }

    public Task UpdateAsync()
    {
        return Task.CompletedTask;
    }

    public Task UnloadAsync()
    {
        return Task.CompletedTask;
    }
}   
