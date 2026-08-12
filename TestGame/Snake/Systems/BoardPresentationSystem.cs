using System.Numerics;
using Cadmus.Core.Components;
using Cadmus.Core.Entities;
using Cadmus.Core.Game;
using Cadmus.Core.Scenes;
using Cadmus.Core.Systems;
using Cadmus.Core.Windowing;
using Cadmus.Engine.Components;
using Cadmus.Engine.Components.Sprites;
using Cadmus.Engine.Scenes;
using TestGame.Snake.Components;

namespace TestGame.Snake.Systems;

/// <summary>
/// The only system that touches sprites: it turns grid data into what the renderer draws. Keeping
/// presentation separate is what lets the rules above be tested and changed without any notion of
/// pixels.
/// </summary>
public sealed class BoardPresentationSystem(
    ISceneManager scenes,
    IGameWindow window,
    SnakeSettings settings
) : ISystem
{
    private const string Texture = "Assets/Textures/white.png";

    private readonly Dictionary<Guid, int> syncedRevisions = [];

    public int Order => SystemOrder.Presentation;

    public ValueTask UpdateAsync(GameTime time, CancellationToken cancellationToken = default)
    {
        var scene = scenes.Current;
        var board = scene.Single<BoardComponent>();

        if (board is null)
        {
            return ValueTask.CompletedTask;
        }

        var (width, height) = window.FramebufferSize;
        board.Origin = new Vector2(
            (width - settings.BoardWidth) / 2f,
            (height - settings.BoardHeight) / 2f
        );

        var state = scene.Single<GameStateComponent>();
        var isDead = state?.IsDead ?? false;

        foreach (var (entity, _) in scene.Query<BoardComponent>())
        {
            SetOrigin(entity, board.Origin);
        }

        // How far the current move has progressed, 0 to 1. A dead or paused snake holds its cells.
        var progress = state is null || isDead || !settings.SmoothMovement
            ? 1f
            : Math.Clamp(state.TickTimer / settings.TickSecondsFor(state.Score), 0f, 1f);

        foreach (var (entity, body) in scene.Query<SnakeBodyComponent>())
        {
            SetOrigin(entity, board.Origin);
            SyncSnake(entity, body, isDead);
            PlaceSegments(entity, body, progress);
        }

        foreach (var (entity, food) in scene.Query<FoodComponent>())
        {
            SetOrigin(entity, board.Origin);
            SyncFood(entity, food);
        }

        return ValueTask.CompletedTask;
    }

    private static void SetOrigin(IEntity entity, Vector2 origin)
    {
        if (entity.TryGetComponent<PositionComponent>(out var position))
        {
            position.Vector = new Vector3(origin, 0f);
        }
    }

    private void SyncSnake(IEntity entity, SnakeBodyComponent body, bool isDead)
    {
        // Sprites are rebuilt only when the body actually changed; the origin above still tracks
        // the window every frame.
        var revision = HashCode.Combine(body.Revision, isDead);

        if (syncedRevisions.TryGetValue(entity.Id, out var synced) && synced == revision)
        {
            return;
        }

        syncedRevisions[entity.Id] = revision;

        entity.RemoveAllComponents<SpriteComponent>();

        for (int i = 0; i < body.Cells.Count; i++)
        {
            entity.AddComponent(
                new SpriteComponent(
                    Texture,
                    settings.SpriteSize,
                    new PositionComponent(settings.CellCenter(body.Cells[i], 1f))
                )
                {
                    Tint = SegmentColor(body, i, isDead)
                }
            );
        }
    }

    /// <summary>
    /// Slides every segment from where it was towards where it is. Each one follows the segment
    /// ahead of it, which is what reads as a snake gliding rather than a row of jumping squares.
    /// </summary>
    private void PlaceSegments(IEntity entity, SnakeBodyComponent body, float progress)
    {
        var index = 0;

        foreach (var sprite in entity.GetComponents<SpriteComponent>())
        {
            if (index >= body.Cells.Count)
            {
                break;
            }

            var from = settings.CellCenter(body.PreviousOf(index), 1f);
            var to = settings.CellCenter(body.Cells[index], 1f);

            if (sprite.TryGetComponent<PositionComponent>(out var position))
            {
                position.Vector = Vector3.Lerp(from, to, progress);
            }

            index++;
        }
    }

    private void SyncFood(IEntity entity, FoodComponent food)
    {
        if (syncedRevisions.TryGetValue(entity.Id, out var synced) && synced == food.Revision)
        {
            return;
        }

        syncedRevisions[entity.Id] = food.Revision;

        entity.RemoveAllComponents<SpriteComponent>();
        entity.AddComponent(
            new SpriteComponent(
                Texture,
                settings.SpriteSize,
                new PositionComponent(settings.CellCenter(food.Cell, 1f))
            )
            {
                Tint = settings.FoodColor
            }
        );
    }

    private Vector4 SegmentColor(SnakeBodyComponent body, int index, bool isDead)
    {
        if (isDead)
        {
            return settings.DeadColor;
        }

        if (index == 0)
        {
            return settings.HeadColor;
        }

        // Fade from body to tail so the direction of travel reads at a glance.
        var t = body.Cells.Count <= 2 ? 0f : (index - 1) / (float)(body.Cells.Count - 2);

        return Vector4.Lerp(settings.BodyColor, settings.TailColor, t);
    }
}
