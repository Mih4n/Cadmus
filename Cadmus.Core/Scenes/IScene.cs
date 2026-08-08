using Cadmus.Core.Components;
using Cadmus.Core.Entities;
using Cadmus.Core.Game;

namespace Cadmus.Core.Scenes;

public interface IScene : IComposeComponent
{
    string Name { get; }
    IReadOnlyDictionary<Guid, IEntity> Entities { get; }

    IScene AddEntity(IEntity entity);
    bool RemoveEntity(Guid entityId);
    IEntity? GetEntity(Guid entityId);

    Task LoadAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(GameTime time, CancellationToken cancellationToken = default);
    Task UnloadAsync(CancellationToken cancellationToken = default);
}
