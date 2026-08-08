using Cadmus.Core.Components;
using Cadmus.Core.Entities;
using Cadmus.Engine.Components;

namespace Cadmus.Engine.Entities;

/// <summary>
/// Base entity. Derived entities are created through <see cref="IEntityFactory"/>, so they may take
/// services as constructor parameters and let the container fill them in.
/// </summary>
public class Entity : ComposeComponent, IEntity
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public string Name { get; set; }
    public bool IsEnabled { get; set; } = true;

    public Entity(params IEnumerable<IComponent> components)
    {
        Name = GetType().Name;
        AddComponents(components);
    }

    public Entity(string name, params IEnumerable<IComponent> components)
    {
        Name = name;
        AddComponents(components);
    }

    /// <summary>World position of the entity; the origin when no position component is attached.</summary>
    public System.Numerics.Vector3 Position =>
        GetComponent<PositionComponent>()?.Vector ?? System.Numerics.Vector3.Zero;
}
