using Cadmus.Domain;
using Cadmus.Domain.Contracts;

namespace Cadmus.App.Systems.Base;

public class ButtonTrackingSystem : ISystem
{
    private readonly HashSet<ConsoleKey> pressedKeys = new();

    public Task Update(IGameContext context)
    {
        // Check for key presses
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;

            // Handle quit
            if (key == ConsoleKey.Q)
            {
                Environment.Exit(0);
            }

            // Track pressed keys
            pressedKeys.Add(key);

            // Update snake direction based on input
            UpdateSnakeDirection(context, key);
        }

        return Task.CompletedTask;
    }

    private void UpdateSnakeDirection(IGameContext context, ConsoleKey key)
    {
        var currentScene = context.Scene;
        if (currentScene != null)
        {
            foreach (var entityPair in currentScene.Entities)
            {
                var entity = entityPair.Value;

                // Find snake head and update direction
                if (entity.TryGetComponent<SnakeSegmentComponent>(out var snakeSegment) && snakeSegment.IsHead)
                {
                    if (entity.TryGetComponent<DirectionComponent>(out var direction))
                    {
                        switch (key)
                        {
                            case ConsoleKey.W:
                            case ConsoleKey.UpArrow:
                                if (direction.DY == 0) // Prevent immediate reversal
                                {
                                    direction.DX = 0;
                                    direction.DY = -1;
                                }
                                break;
                            case ConsoleKey.S:
                            case ConsoleKey.DownArrow:
                                if (direction.DY == 0)
                                {
                                    direction.DX = 0;
                                    direction.DY = 1;
                                }
                                break;
                            case ConsoleKey.A:
                            case ConsoleKey.LeftArrow:
                                if (direction.DX == 0)
                                {
                                    direction.DX = -1;
                                    direction.DY = 0;
                                }
                                break;
                            case ConsoleKey.D:
                            case ConsoleKey.RightArrow:
                                if (direction.DX == 0)
                                {
                                    direction.DX = 1;
                                    direction.DY = 0;
                                }
                                break;
                        }
                    }
                }
            }
        }
    }

    public bool IsKeyPressed(ConsoleKey key)
    {
        return pressedKeys.Contains(key);
    }

    public void ClearPressedKeys()
    {
        pressedKeys.Clear();
    }
}