namespace Cadmus.Core.Game;

/// <summary>
/// Owns the frame loop: pumps the window, updates systems in order, then renders.
/// </summary>
public interface IGameHost
{
    bool IsRunning { get; }
    GameTime Time { get; }

    Task RunAsync(CancellationToken cancellationToken = default);
    void Stop();
}
