using Cadmus.Engine.Components;

namespace Tetris.Components;

/// <summary>
/// The piece currently falling. It exists only while it falls: locking removes the entity and the
/// spawn system adds the next one, so "is there a piece in play" is answered by a query.
/// </summary>
public sealed class PieceComponent : Component
{
    public PieceKind Kind { get; init; }

    /// <summary>Origin of the piece's rotation box on the grid.</summary>
    public Cell Cell { get; set; }

    /// <summary>Rotation state, 0 to 3.</summary>
    public int Rotation { get; set; }

    /// <summary>Where a hard drop would land it; filled by the gravity system, drawn as the ghost.</summary>
    public Cell LandingCell { get; set; }

    /// <summary>True when there is no room below, which is when the lock delay runs.</summary>
    public bool IsLanded { get; set; }

    public float LockTimer { get; set; }

    /// <summary>How many times a move has already refreshed the lock delay.</summary>
    public int LockResets { get; set; }

    /// <summary>
    /// How far the piece has travelled towards the next row, 0 to 1, and 0 while it rests. Movement
    /// stays discrete — this is what lets presentation draw the fall as continuous.
    /// </summary>
    public float FallProgress { get; set; }
}
