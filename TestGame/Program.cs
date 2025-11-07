using Cadmus.Domain;

public class SnakeGame : Game
{
    private const int GridWidth = 20;
    private const int GridHeight = 20;
    private int score = 0;

    public override async Task InitializeAsync()
    {
        SetSystem(new VulkanRenderer());
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
        }
    }
}
