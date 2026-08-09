using Cadmus.Engine.Components;

namespace TestGame.Snake.Components;

/// <summary>
/// The match's state, kept on a single entity so systems reach it through a query instead of
/// through each other.
/// </summary>
public sealed class GameStateComponent : Component
{
    public int Score { get; set; }
    public bool IsDead { get; set; }
    public bool IsPaused { get; set; }

    /// <summary>Seconds accumulated towards the next move.</summary>
    public float TickTimer { get; set; }

    public bool IsRunning => !IsDead && !IsPaused;
}
