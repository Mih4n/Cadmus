using Cadmus.Core.Events;
using Cadmus.Core.Game;
using Cadmus.Core.Scenes;
using Cadmus.Core.Systems;
using Cadmus.Engine.Scenes;
using TestGame.Snake.Components;
using TestGame.Snake.Events;

namespace TestGame.Snake.Systems;

/// <summary>
/// Owns the match: death, pause and restart. All three arrive as events, so nothing here polls the
/// keyboard or inspects collision rules.
/// </summary>
public sealed class GameFlowSystem(
    ISceneManager scenes,
    IEventQueue events,
    SnakeSettings settings
) : ISystem
{
    public int Order => SystemOrder.Flow;

    public ValueTask UpdateAsync(GameTime time, CancellationToken cancellationToken = default)
    {
        var scene = scenes.Current;
        var state = scene.Single<GameStateComponent>();

        if (state is null)
        {
            return ValueTask.CompletedTask;
        }

        if (events.Has<SnakeDied>())
        {
            state.IsDead = true;
        }

        // Restart wins over pause: pressing space on the game-over screen should just play again.
        if (events.Has<RestartRequested>())
        {
            Restart(scene, state);
        }
        else if (events.Has<PauseToggled>() && !state.IsDead)
        {
            state.IsPaused = !state.IsPaused;
        }

        return ValueTask.CompletedTask;
    }

    private void Restart(IScene? scene, GameStateComponent state)
    {
        state.Score = 0;
        state.IsDead = false;
        state.IsPaused = false;
        state.TickTimer = 0f;

        foreach (var (entity, body) in scene.Query<SnakeBodyComponent>())
        {
            ResetBody(body);

            if (entity.TryGetComponent<PlayerControlComponent>(out var control))
            {
                control.RequestedHeading = null;
            }
        }

        // Announced only now that the bodies are back at their start, so whoever places food sees
        // the board it will actually be played on.
        events.Publish(new MatchRestarted());
    }

    private void ResetBody(SnakeBodyComponent body)
    {
        body.Cells.Clear();

        var start = new Cell(settings.Columns / 4, settings.Rows / 2);
        for (int i = 0; i < settings.StartLength; i++)
        {
            body.Cells.Add(new Cell(start.X - i, start.Y));
        }

        body.Heading = Cell.Right;
        body.Revision++;
    }
}
