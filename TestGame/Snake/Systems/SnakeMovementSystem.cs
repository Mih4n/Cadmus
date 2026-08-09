using Cadmus.Core.Entities;
using Cadmus.Core.Events;
using Cadmus.Core.Game;
using Cadmus.Core.Scenes;
using Cadmus.Core.Systems;
using Cadmus.Engine.Scenes;
using TestGame.Snake.Components;
using TestGame.Snake.Events;

namespace TestGame.Snake.Systems;

/// <summary>
/// Moves every snake on the match tick. It reports what happened as events and never reaches into
/// scoring or food placement itself.
/// </summary>
public sealed class SnakeMovementSystem(
    ISceneManager scenes,
    IEventQueue events,
    SnakeSettings settings
) : ISystem
{
    public int Order => SystemOrder.Movement;

    public ValueTask UpdateAsync(GameTime time, CancellationToken cancellationToken = default)
    {
        var scene = scenes.Current;
        var state = scene.Single<GameStateComponent>();

        if (state is null || !state.IsRunning)
        {
            return ValueTask.CompletedTask;
        }

        state.TickTimer += time.DeltaSeconds;

        var tickSeconds = MathF.Max(
            settings.MinTickSeconds,
            settings.StartTickSeconds - state.Score * settings.TickSpeedUp
        );

        if (state.TickTimer < tickSeconds)
        {
            return ValueTask.CompletedTask;
        }

        state.TickTimer = 0f;

        foreach (var (entity, body) in scene.Query<SnakeBodyComponent>())
        {
            // The turn requested since the last tick becomes the heading now, so input during a
            // tick cannot make the snake move twice in one direction.
            if (entity.TryGetComponent<PlayerControlComponent>(out var control) &&
                control.RequestedHeading is { } requested)
            {
                body.Heading = requested;
                control.RequestedHeading = null;
            }

            Step(scene, entity, body);
        }

        return ValueTask.CompletedTask;
    }

    private void Step(IScene? scene, IEntity entity, SnakeBodyComponent body)
    {
        var next = body.Head + body.Heading;
        var food = FindFoodAt(scene, next);
        var eating = food is not null;

        if (!settings.Contains(next))
        {
            events.Publish(new SnakeDied(entity, next, DeathCause.Wall));
            return;
        }

        // The tail vacates its cell on this same tick, so it is only an obstacle when the snake is
        // about to grow and the tail therefore stays put.
        if (body.Occupies(next, ignoreTail: !eating))
        {
            events.Publish(new SnakeDied(entity, next, DeathCause.Self));
            return;
        }

        body.Cells.Insert(0, next);

        if (!eating)
        {
            body.Cells.RemoveAt(body.Cells.Count - 1);
        }

        body.Revision++;

        if (food is not null)
        {
            events.Publish(new FoodEaten(entity, food.Value.Entity, next));
        }
    }

    private static (IEntity Entity, FoodComponent Food)? FindFoodAt(IScene? scene, Cell cell)
    {
        foreach (var (entity, food) in scene.Query<FoodComponent>())
        {
            if (food.Cell == cell)
            {
                return (entity, food);
            }
        }

        return null;
    }
}
