using Cadmus.Engine.Components;

namespace Tetris.Components;

public enum ClearPhase
{
    None,

    /// <summary>The full rows glow and shrink, still in place.</summary>
    Flash,

    /// <summary>The rows are gone and everything above is sliding into its new place.</summary>
    Collapse
}

/// <summary>
/// A line clear in progress. It is two timed phases rather than an instant edit so the board can be
/// seen to react; while it runs the match is <see cref="GameStateComponent.IsResolving"/>.
/// </summary>
public sealed class LineClearComponent : Component
{
    public List<int> Rows { get; } = [];

    public ClearPhase Phase { get; set; } = ClearPhase.None;

    public float Timer { get; set; }

    public bool IsFlashing(int row) => Phase == ClearPhase.Flash && Rows.Contains(row);

    public void Reset()
    {
        Rows.Clear();
        Phase = ClearPhase.None;
        Timer = 0f;
    }
}
