using Cadmus.Core.Input;
using Cadmus.Engine.Components;

namespace Tetris.Components;

/// <summary>
/// Marks an entity as the player and says which keys it answers to. The auto-repeat state lives here
/// too, because it is per-player data — the input system is the behaviour that advances it.
/// </summary>
public sealed class PlayerControlComponent : Component
{
    public Key[] Left { get; init; } = [Key.Left, Key.A];
    public Key[] Right { get; init; } = [Key.Right, Key.D];
    public Key[] RotateClockwise { get; init; } = [Key.Up, Key.W, Key.X];
    public Key[] RotateCounterClockwise { get; init; } = [Key.Z];
    public Key[] SoftDrop { get; init; } = [Key.Down, Key.S];
    public Key[] HardDrop { get; init; } = [Key.Space];
    public Key[] Hold { get; init; } = [Key.C];

    /// <summary>-1, 0 or 1: which way the player is holding right now.</summary>
    public int HeldDirection { get; set; }

    /// <summary>Counts down to the next automatic shift while a direction stays held.</summary>
    public float RepeatTimer { get; set; }

    public bool IsSoftDropping { get; set; }
}
