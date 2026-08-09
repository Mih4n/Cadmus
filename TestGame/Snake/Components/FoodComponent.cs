using Cadmus.Engine.Components;

namespace TestGame.Snake.Components;

/// <summary>An edible cell.</summary>
public sealed class FoodComponent : Component
{
    public Cell Cell { get; set; }

    /// <summary>Bumped when the cell changes, so presentation knows to move the sprite.</summary>
    public int Revision { get; set; }
}
