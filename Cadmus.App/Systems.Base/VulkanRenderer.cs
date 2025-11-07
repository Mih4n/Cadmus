using Cadmus.Domain;
using Cadmus.Domain.Contracts;

namespace Cadmus.App.Systems.Base;

public class VulkanRenderer : ISystem, IDisposable
{
    private const int Width = 20;
    private const int Height = 20;
    private char[,]? screenBuffer;
    private bool initialized;

    public Task Update(IGameContext context)
    {
        if (!initialized)
        {
            InitializeRenderer();
            initialized = true;
        }

        RenderFrame(context);
        return Task.CompletedTask;
    }

    private void InitializeRenderer()
    {
        screenBuffer = new char[Height, Width];
        Console.Clear();
        Console.WriteLine("Simple Console Renderer Initialized");
        Console.WriteLine("Snake Game - Use WASD to move, Q to quit");
        Console.WriteLine();
    }

    private void RenderFrame(IGameContext context)
    {
        if (screenBuffer == null) return;

        // Clear screen buffer
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                screenBuffer[y, x] = '.';
            }
        }

        // Render entities from current scene
        var currentScene = context.Scene;
        if (currentScene != null)
        {
            foreach (var entityPair in currentScene.Entities)
            {
                var entity = entityPair.Value;

                // Check for PositionComponent and render based on other components
                if (entity.TryGetComponent<PositionComponent>(out var position))
                {
                    // Clamp position to screen bounds
                    int renderX = Math.Clamp(position.X, 0, Width - 1);
                    int renderY = Math.Clamp(position.Y, 0, Height - 1);

                    // Determine what to render based on components
                    if (entity.TryGetComponent<SnakeSegmentComponent>(out var snakeSegment))
                    {
                        screenBuffer[renderY, renderX] = snakeSegment.IsHead ? '@' : 'o';
                    }
                    else if (entity.HasComponent<FoodComponent>())
                    {
                        screenBuffer[renderY, renderX] = '*';
                    }
                }
            }
        }

        // Draw to console
        Console.Clear();
        Console.SetCursorPosition(0, 3);
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Console.Write(screenBuffer[y, x]);
            }
            Console.WriteLine();
        }
    }

    public void Dispose()
    {
        if (initialized)
        {
            Console.WriteLine("Renderer disposed");
        }
    }
}

// Define components here since they're used by the renderer
public class PositionComponent : IComponent
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class DirectionComponent : IComponent
{
    public int DX { get; set; }
    public int DY { get; set; }
}

public class SnakeSegmentComponent : IComponent
{
    public bool IsHead { get; set; }
}

public class FoodComponent : IComponent { }