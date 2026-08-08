using System.Numerics;
using Cadmus.Core.Entities;
using Cadmus.Core.Game;
using Cadmus.Core.Input;
using Cadmus.Core.Windowing;
using Cadmus.Engine.Components;
using Cadmus.Engine.Components.Sprites;
using Cadmus.Engine.Entities;
using Cadmus.Engine.Scenes;

namespace TestGame.Snake;

/// <summary>
/// Drives the game: ticks the snake on a fixed interval, feeds it, kills it and restarts it. Every
/// collaborator arrives through the constructor.
/// </summary>
public sealed class SnakeScene(
    IEntityFactory entities,
    IGameWindow window,
    IInputService input,
    SnakeSettings settings
) : Scene(entities)
{
    private readonly Random random = new();

    private SnakeEntity snake = null!;
    private FoodEntity food = null!;
    private IEntity board = null!;

    private float tickTimer;
    private float titleTimer;

    public int Score { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsPaused { get; private set; }

    public override Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var center = new Vector3(settings.BoardWidth / 2f, settings.BoardHeight / 2f, 0f);

        // Two stacked quads: the frame sits behind (z 0) and the field in front of it (z 0.5),
        // both behind the snake and the apple at z 1.
        board = Spawn(
            "Board",
            new PositionComponent(),
            new SpriteComponent(
                "Assets/Textures/white.png",
                new Vector2(
                    settings.BoardWidth + settings.BorderWidth * 2,
                    settings.BoardHeight + settings.BorderWidth * 2
                ),
                new PositionComponent(center)
            )
            {
                Tint = settings.BorderColor
            },
            new SpriteComponent(
                "Assets/Textures/white.png",
                new Vector2(settings.BoardWidth, settings.BoardHeight),
                new PositionComponent(center with { Z = 0.5f })
            )
            {
                Tint = settings.BoardColor
            }
        );

        snake = Spawn<SnakeEntity>();
        food = Spawn<FoodEntity>();

        StartNewGame();

        return Task.CompletedTask;
    }

    public override Task UpdateAsync(GameTime time, CancellationToken cancellationToken = default)
    {
        HandleGameKeys();

        if (!IsDead && !IsPaused)
        {
            snake.ReadInput();

            tickTimer += time.DeltaSeconds;
            if (tickTimer >= CurrentTickSeconds)
            {
                tickTimer = 0f;
                Step();
            }
        }

        SyncLayout();
        UpdateTitle(time);

        return Task.CompletedTask;
    }

    private float CurrentTickSeconds => MathF.Max(
        settings.MinTickSeconds,
        settings.StartTickSeconds - Score * settings.TickSpeedUp
    );

    private void HandleGameKeys()
    {
        if (IsDead)
        {
            if (input.WasKeyPressed(Key.Space) || input.WasKeyPressed(Key.Enter) || input.WasKeyPressed(Key.R))
            {
                StartNewGame();
            }

            return;
        }

        if (input.WasKeyPressed(Key.P))
        {
            IsPaused = !IsPaused;
        }

        if (input.WasKeyPressed(Key.R))
        {
            StartNewGame();
        }
    }

    private void StartNewGame()
    {
        Score = 0;
        IsDead = false;
        IsPaused = false;
        tickTimer = 0f;

        snake.Reset();
        food.MoveTo(FindFreeCell());
    }

    private void Step()
    {
        snake.ApplyTurn();
        var next = snake.NextHead;

        // The tail moves out of the way on this same tick, so it is not an obstacle — unless the
        // snake is about to grow, in which case the tail stays put.
        var eating = next == food.Cell;

        if (!settings.Contains(next) || snake.Occupies(next, ignoreTail: !eating))
        {
            IsDead = true;
            return;
        }

        snake.Advance(next, grow: eating);

        if (eating)
        {
            Score++;
            food.MoveTo(FindFreeCell());
        }
    }

    private Cell FindFreeCell()
    {
        var free = new List<Cell>(settings.Columns * settings.Rows);

        for (int y = 0; y < settings.Rows; y++)
        {
            for (int x = 0; x < settings.Columns; x++)
            {
                var cell = new Cell(x, y);
                if (!snake.Occupies(cell))
                {
                    free.Add(cell);
                }
            }
        }

        // Board full: the player has won, park the apple under the head rather than crash.
        return free.Count == 0 ? snake.Head : free[random.Next(free.Count)];
    }

    /// <summary>Recentres the board every frame so resizing the window keeps it in the middle.</summary>
    private void SyncLayout()
    {
        var (width, height) = window.FramebufferSize;
        var origin = new Vector2(
            (width - settings.BoardWidth) / 2f,
            (height - settings.BoardHeight) / 2f
        );

        board.GetComponent<PositionComponent>()!.Vector = new Vector3(origin, 0f);

        snake.SyncSprites(origin, IsDead);
        food.SyncSprites(origin);
    }

    private void UpdateTitle(GameTime time)
    {
        titleTimer += time.DeltaSeconds;

        if (titleTimer < 0.25f)
        {
            return;
        }

        titleTimer = 0f;

        var state = IsDead
            ? "game over — space to restart"
            : IsPaused ? "paused" : $"{time.FramesPerSecond:F0} FPS";

        window.Title = $"Cadmus Snake — score {Score} — {state}";
    }
}
