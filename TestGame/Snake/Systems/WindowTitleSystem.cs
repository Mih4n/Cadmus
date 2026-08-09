using Cadmus.Core.Diagnostics;
using Cadmus.Core.Game;
using Cadmus.Core.Scenes;
using Cadmus.Core.Systems;
using Cadmus.Core.Windowing;
using Cadmus.Engine.Scenes;
using TestGame.Snake.Components;

namespace TestGame.Snake.Systems;

/// <summary>Reports the score and the match state in the window title.</summary>
public sealed class WindowTitleSystem(
    ISceneManager scenes,
    IGameWindow window,
    IFrameStatistics statistics
) : ISystem
{
    private float timer;

    public int Order => SystemOrder.Presentation + 1;

    public ValueTask UpdateAsync(GameTime time, CancellationToken cancellationToken = default)
    {
        timer += time.DeltaSeconds;

        if (timer < 0.25f)
        {
            return ValueTask.CompletedTask;
        }

        timer = 0f;

        var state = scenes.Current.Single<GameStateComponent>();

        if (state is null)
        {
            return ValueTask.CompletedTask;
        }

        var status = state.IsDead
            ? "game over — space to restart"
            : state.IsPaused ? "paused" : $"{statistics.Fps:F0} FPS";

        window.Title = $"Cadmus Snake — score {state.Score} — {status}";

        return ValueTask.CompletedTask;
    }
}
