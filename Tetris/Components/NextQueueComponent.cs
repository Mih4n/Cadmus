using Cadmus.Engine.Components;

namespace Tetris.Components;

/// <summary>
/// What is coming and what is being kept back. The bag that fills <see cref="Upcoming"/> lives in the
/// spawn system — this only holds the result.
/// </summary>
public sealed class NextQueueComponent : Component
{
    /// <summary>Next piece first.</summary>
    public List<PieceKind> Upcoming { get; } = [];

    public PieceKind? Held { get; set; }

    /// <summary>Holding is allowed once per piece; locking clears this.</summary>
    public bool HoldUsed { get; set; }

    /// <summary>Bumped whenever the queue or the hold changes, so the panels are rebuilt only then.</summary>
    public int Revision { get; set; }
}
