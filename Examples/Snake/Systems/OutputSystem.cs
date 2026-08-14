using Cadmus.Core.Storage;
using Cadmus.Engine;
using Snake.Components;

namespace Snake.Systems;

public class OutputSystem(IEntityStorage storage) : ISystem
{
    public void OnStop() {}
    public void OnStart() {}

    public void Update(float deltaTime)
    {
        Console.WriteLine("OutputSystem updating");
        storage.Query<Position>(positions =>
            {
                Console.WriteLine("Positions:");
                foreach (var position in positions)
                {
                    Console.WriteLine($"({position.X}, {position.Y})");
                }
            }
        );
    }
}
