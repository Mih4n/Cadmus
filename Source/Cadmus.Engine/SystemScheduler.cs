namespace Cadmus.Engine;

public sealed class SystemScheduler(IEnumerable<ISystem> systems)
{
    private readonly List<ISystem> ordered = [.. systems.OrderBy(system => system.Order)];

    public void AddSystem(ISystem system)
    {
        ordered.Add(system);
        ordered.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    public void Update(float deltaTime)
    {
        foreach (var system in ordered)
            system.Update(deltaTime);
    }
}
