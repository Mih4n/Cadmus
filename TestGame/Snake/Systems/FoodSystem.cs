using Cadmus.Core.Events;
using Cadmus.Core.Game;
using Cadmus.Core.Scenes;
using Cadmus.Core.Systems;
using Cadmus.Engine.Scenes;
using TestGame.Snake.Components;
using TestGame.Snake.Events;

namespace TestGame.Snake.Systems;

/// <summary>
/// Reacts to food being eaten: scores it and moves the apple somewhere free. It knows nothing about
/// input or collision — only that the event happened.
/// </summary>
public sealed class FoodSystem(
    ISceneManager scenes,
    IEventQueue events,
    SnakeSettings settings
) : ISystem
{
    private readonly Random random = new();
    private readonly List<Cell> freeCells = [];

    public int Order => SystemOrder.Food;

    public ValueTask UpdateAsync(GameTime time, CancellationToken cancellationToken = default)
    {
        var scene = scenes.Current;

        foreach (var eaten in events.Read<FoodEaten>())
        {
            if (!eaten.Food.TryGetComponent<FoodComponent>(out var food))
            {
                continue;
            }

            food.Cell = FindFreeCell(scene, fallback: food.Cell);
            food.Revision++;

            var state = scene.Single<GameStateComponent>();
            if (state is not null)
            {
                state.Score++;
            }
        }

        // A restart puts the apple back on the board along with everything else.
        if (events.Has<MatchRestarted>())
        {
            foreach (var (_, food) in scene.Query<FoodComponent>())
            {
                food.Cell = FindFreeCell(scene, fallback: food.Cell);
                food.Revision++;
            }
        }

        return ValueTask.CompletedTask;
    }

    private Cell FindFreeCell(IScene? scene, Cell fallback)
    {
        freeCells.Clear();

        for (int y = 0; y < settings.Rows; y++)
        {
            for (int x = 0; x < settings.Columns; x++)
            {
                var cell = new Cell(x, y);

                if (!IsOccupied(scene, cell))
                {
                    freeCells.Add(cell);
                }
            }
        }

        // Board full: the player has effectively won, so leave the apple where it is.
        return freeCells.Count == 0 ? fallback : freeCells[random.Next(freeCells.Count)];
    }

    private static bool IsOccupied(IScene? scene, Cell cell)
    {
        foreach (var (_, body) in scene.Query<SnakeBodyComponent>())
        {
            if (body.Occupies(cell))
            {
                return true;
            }
        }

        return false;
    }
}
