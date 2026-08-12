using Cadmus.Engine.Components;

namespace Tetris.Components;

/// <summary>
/// The match's state, kept on a single entity so systems reach it through a query instead of through
/// each other.
/// </summary>
public sealed class GameStateComponent : Component
{
    public int Score { get; set; }
    public int Lines { get; set; }
    public int Level { get; set; } = 1;

    public bool IsDead { get; set; }
    public bool IsPaused { get; set; }

    /// <summary>True while rows are flashing or collapsing: gravity and spawning wait for it.</summary>
    public bool IsResolving { get; set; }

    /// <summary>Seconds accumulated towards the next row.</summary>
    public float GravityTimer { get; set; }

    /// <summary>
    /// Interval the timer above is measured against, published by the gravity system because it also
    /// depends on whether soft drop is held.
    /// </summary>
    public float GravityInterval { get; set; }

    public bool IsRunning => !IsDead && !IsPaused && !IsResolving;
}
