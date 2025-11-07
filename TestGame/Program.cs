using Cadmus.Domain;
using Cadmus.Domain.Contracts;
using Cadmus.App.Systems.Base;

public class SnakeGame : Game
{
    private const int GridWidth = 20;
    private const int GridHeight = 20;
    private int score = 0;

    public override async Task InitializeAsync()
    {
        // Register systems
        SetSystem(new VulkanRenderer());
        SetSystem(new ButtonTrackingSystem());

        // Create scene
        var scene = new Scene();
        RegisterScene("MainScene", scene);

        // Create snake head
        var snakeHead = new Entity();
        snakeHead.AddComponents(
            new PositionComponent { X = GridWidth / 2, Y = GridHeight / 2 },
            new DirectionComponent { DX = 0, DY = -1 },
            new SnakeSegmentComponent { IsHead = true }
        );
        scene.AddEntity(snakeHead);

        // Create initial food
        SpawnFood(scene);

        await LoadSceneAsync("MainScene");
    }

    private void SpawnFood(IScene scene)
    {
        var random = new Random();
        var food = new Entity();
        food.AddComponents(
            new PositionComponent { X = random.Next(0, GridWidth), Y = random.Next(0, GridHeight) },
            new FoodComponent()
        );
        scene.AddEntity(food);
    }

    public void UpdateSnakeMovement(IGameContext context)
    {
        var currentScene = context.Scene;
        if (currentScene != null)
        {
            foreach (var entityPair in currentScene.Entities)
            {
                var entity = entityPair.Value;

                // Move snake head
                if (entity.TryGetComponent<SnakeSegmentComponent>(out var snakeSegment) && snakeSegment.IsHead)
                {
                    if (entity.TryGetComponent<PositionComponent>(out var position) &&
                        entity.TryGetComponent<DirectionComponent>(out var direction))
                    {
                        position.X += direction.DX;
                        position.Y += direction.DY;

                        // Wrap around screen edges
                        if (position.X < 0) position.X = GridWidth - 1;
                        if (position.X >= GridWidth) position.X = 0;
                        if (position.Y < 0) position.Y = GridHeight - 1;
                        if (position.Y >= GridHeight) position.Y = 0;
                    }
                }
            }

            // Check for food collision
            CheckFoodCollision(currentScene);
        }
    }

    private void CheckFoodCollision(IScene scene)
    {
        IEntity? snakeHead = null;
        IEntity? food = null;

        foreach (var entityPair in scene.Entities)
        {
            var entity = entityPair.Value;

            if (entity.TryGetComponent<SnakeSegmentComponent>(out var snakeSegment) && snakeSegment.IsHead)
            {
                snakeHead = entity;
            }
            else if (entity.HasComponent<FoodComponent>())
            {
                food = entity;
            }
        }

        if (snakeHead != null && food != null)
        {
            if (snakeHead.TryGetComponent<PositionComponent>(out var snakePos) &&
                food.TryGetComponent<PositionComponent>(out var foodPos))
            {
                if (snakePos.X == foodPos.X && snakePos.Y == foodPos.Y)
                {
                    // Eat food
                    scene.RemoveEntity(food.Id);
                    SpawnFood(scene);
                    score++;
                    Console.SetCursorPosition(0, 1);
                    Console.WriteLine($"Score: {score}");
                }
            }
        }
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var game = new SnakeGame();
        await game.InitializeAsync();

        // Game loop
        while (true)
        {
            await game.Update();

            // Update snake movement (this should be in a movement system, but for simplicity)
            if (game.CurrentScene != null)
            {
                var snakeGame = game;
                snakeGame.UpdateSnakeMovement(new GameContext(game));
            }

            await Task.Delay(200); // Snake movement speed
        }
    }
}
