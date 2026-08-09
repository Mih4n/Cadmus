using System.Numerics;
using Cadmus.Core.Entities;
using Cadmus.Engine.Components;
using Cadmus.Engine.Components.Sprites;
using Cadmus.Engine.Scenes;
using TestGame.Snake.Components;

namespace TestGame.Snake;

/// <summary>
/// Composition only: which entities exist and which components they carry. All behaviour lives in
/// the systems under <c>Snake/Systems</c>, so this scene has no <c>UpdateAsync</c> at all.
/// </summary>
public sealed class SnakeScene(IEntityFactory entities, SnakeSettings settings) : Scene(entities)
{
    private const string Texture = "Assets/Textures/white.png";

    public override Task LoadAsync(CancellationToken cancellationToken = default)
    {
        SpawnBoard();
        SpawnSnake();
        SpawnFood();

        Spawn("GameState", new GameStateComponent());

        return Task.CompletedTask;
    }

    private void SpawnBoard()
    {
        var center = new Vector3(settings.BoardWidth / 2f, settings.BoardHeight / 2f, 0f);

        // Two stacked quads: the frame behind at z 0, the field in front of it at z 0.5, both
        // behind the snake and the apple at z 1.
        Spawn(
            "Board",
            new BoardComponent(),
            new PositionComponent(),
            new SpriteComponent(
                Texture,
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
                Texture,
                new Vector2(settings.BoardWidth, settings.BoardHeight),
                new PositionComponent(center with { Z = 0.5f })
            )
            {
                Tint = settings.BoardColor
            }
        );
    }

    private void SpawnSnake()
    {
        var body = new SnakeBodyComponent();
        var start = new Cell(settings.Columns / 4, settings.Rows / 2);

        for (int i = 0; i < settings.StartLength; i++)
        {
            body.Cells.Add(new Cell(start.X - i, start.Y));
        }

        // PlayerControlComponent is what makes the input system pick this entity up; drop it and the
        // snake simply stops taking orders.
        Spawn(
            "Snake",
            new PositionComponent(),
            body,
            new PlayerControlComponent()
        );
    }

    private void SpawnFood()
    {
        Spawn(
            "Food",
            new PositionComponent(),
            new FoodComponent { Cell = new Cell(settings.Columns * 3 / 4, settings.Rows / 2) }
        );
    }
}
