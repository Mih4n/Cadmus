namespace Cadmus.Core.Game;

/// <summary>
/// The user-supplied entry point of a Cadmus application. Implementations are resolved from the
/// service provider, so they declare whatever they need (scene manager, entity factory, own
/// services) as constructor parameters.
/// </summary>
public interface IGame
{
    /// <summary>Runs once before the first frame: register scenes, load the initial one.</summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs once per frame, after all systems have been updated.</summary>
    Task UpdateAsync(GameTime time, CancellationToken cancellationToken = default);

    /// <summary>Runs once after the loop has stopped, before services are disposed.</summary>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
