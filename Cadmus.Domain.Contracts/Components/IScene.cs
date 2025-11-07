using Cadmus.Domain.Contracts.Entities;

namespace Cadmus.Domain.Contracts.Components;

public interface IScene : IComposeComponent
{
    IReadOnlyDictionary<Guid, IEntity> Entities { get; }

    void AddEntity(IEntity entity);
    bool RemoveEntity(Guid entityId);
    Task LoadAsync();
    Task UpdateAsync();
    Task UnloadAsync();
    IEntity? GetEntity(Guid entityId);
}
