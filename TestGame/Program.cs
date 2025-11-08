using System.Numerics;
using Cadmus.App;
using Cadmus.Domain.Components;
using Cadmus.Domain.Components.Sprites;
using Cadmus.Domain.Entities;

public class SnakeGame : Game
{
    public override async Task InitializeAsync()
    {
        RegisterScene("Main", new Scene());

        await LoadSceneAsync("Main");

        if (CurrentScene is null) throw new Exception("No scene defined");

        CurrentScene.AddEntity(new Entity(
                new SpriteComponent(
                    "./Sprites/Test.png",
                    new Vector2(10, 10),
                    new PositionComponent(10, 10, 1)
                ),
                new SpriteComponent(
                    "./Sprites/Test2.png",
                    new Vector2(10, 10),
                    new PositionComponent(0, 0, 0)
                ),
                new PositionComponent()
            )
        );
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
