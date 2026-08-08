using Cadmus.Core.Entities;
using Cadmus.Core.Game;
using Cadmus.Core.Scenes;
using Cadmus.Core.Windowing;

namespace Cadmus.App;

/// <summary>
/// Convenience base for a game. It holds nothing itself — scene switching, entity creation and the
/// window all arrive through the constructor, so a subclass can add its own services the same way.
/// </summary>
public abstract class Game(ISceneManager scenes, IEntityFactory entities, IGameWindow window) : IGame
{
    protected ISceneManager Scenes { get; } = scenes;
    protected IEntityFactory Entities { get; } = entities;
    protected IGameWindow Window { get; } = window;

    protected IScene? CurrentScene => Scenes.Current;

    public abstract Task InitializeAsync(CancellationToken cancellationToken = default);

    public virtual Task UpdateAsync(GameTime time, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public virtual Task ShutdownAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    protected Task LoadSceneAsync(string name, CancellationToken cancellationToken = default) =>
        Scenes.LoadAsync(name, cancellationToken);
}
