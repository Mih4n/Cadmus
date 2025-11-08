using Cadmus.Domain.Contracts.Entities;

namespace Cadmus.Domain.Contracts.Components;

public interface IScene : IComposeComponent
{
    IReadOnlyDictionary<Guid, IEntity> Entities { get; }

    IScene AddEntity(IEntity entity);
    IScene RemoveEntity(Guid entityId);
    Task LoadAsync();
    Task UpdateAsync();
    Task UnloadAsync();
    IEntity? GetEntity(Guid entityId);
}
