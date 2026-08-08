namespace Cadmus.Core.Game;

/// <summary>
/// Timing information for a single frame, passed to every system and scene update.
/// </summary>
public readonly record struct GameTime(TimeSpan Total, TimeSpan Delta, long FrameIndex)
{
    public float TotalSeconds => (float)Total.TotalSeconds;
    public float DeltaSeconds => (float)Delta.TotalSeconds;
    public float FramesPerSecond => Delta > TimeSpan.Zero ? 1f / DeltaSeconds : 0f;
}
