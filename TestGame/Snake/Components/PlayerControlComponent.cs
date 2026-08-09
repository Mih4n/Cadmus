using Cadmus.Engine.Components;
using Cadmus.Core.Input;

namespace TestGame.Snake.Components;

/// <summary>
/// Marks an entity as driven by the player, and says which keys steer it. Attach it to a second
/// entity with a different binding set and the same system drives both.
/// </summary>
public sealed class PlayerControlComponent : Component
{
    public Key[] Up { get; init; } = [Key.Up, Key.W];
    public Key[] Down { get; init; } = [Key.Down, Key.S];
    public Key[] Left { get; init; } = [Key.Left, Key.A];
    public Key[] Right { get; init; } = [Key.Right, Key.D];

    /// <summary>Direction requested since the last tick, applied when the snake next moves.</summary>
    public Cell? RequestedHeading { get; set; }
}
