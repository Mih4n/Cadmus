using System.Diagnostics;
using Cadmus.Core.Game;
using Cadmus.Core.Scenes;
using Cadmus.Core.Systems;
using Cadmus.Core.Windowing;

namespace Cadmus.App;

/// <summary>
/// The frame loop. Systems are updated in <see cref="ISystem.Order"/> order — sequentially, because
/// they share scene state — and renderers submit afterwards.
/// </summary>
public sealed class GameHost : IGameHost
{
    private readonly IGame game;
    private readonly IGameWindow window;
    private readonly ISceneManager scenes;
    private readonly ISystem[] systems;
    private readonly IRenderSystem[] renderers;

    private bool stopRequested;

    public bool IsRunning { get; private set; }
    public GameTime Time { get; private set; }

    public GameHost(IGame game, IGameWindow window, ISceneManager scenes, IEnumerable<ISystem> systems)
    {
        this.game = game;
        this.window = window;
        this.scenes = scenes;
        this.systems = [.. systems.OrderBy(s => s.Order)];
        renderers = [.. this.systems.OfType<IRenderSystem>()];
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await game.InitializeAsync(cancellationToken);

        IsRunning = true;
        var clock = Stopwatch.StartNew();
        var previous = TimeSpan.Zero;
        long frameIndex = 0;

        try
        {
            while (IsRunning && !stopRequested && !cancellationToken.IsCancellationRequested)
            {
                window.PollEvents();

                if (window.IsClosing)
                {
                    break;
                }

                var total = clock.Elapsed;
                Time = new GameTime(total, total - previous, frameIndex++);
                previous = total;

                foreach (var system in systems)
                {
                    await system.UpdateAsync(Time, cancellationToken);
                }

                if (scenes.Current is { } scene)
                {
                    await scene.UpdateAsync(Time, cancellationToken);
                }

                await game.UpdateAsync(Time, cancellationToken);

                foreach (var renderer in renderers)
                {
                    renderer.Render(Time);
                }
            }
        }
        finally
        {
            IsRunning = false;
            await game.ShutdownAsync(CancellationToken.None);
        }
    }

    public void Stop()
    {
        stopRequested = true;
        window.Close();
    }
}
