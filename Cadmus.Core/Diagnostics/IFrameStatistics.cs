namespace Cadmus.Core.Diagnostics;

/// <summary>
/// What the engine measured about the frame that just ran. Anything can inject this — the debug
/// overlay is only one consumer.
/// </summary>
public interface IFrameStatistics
{
    /// <summary>Smoothed over the sample window, not the instantaneous 1/delta.</summary>
    float Fps { get; }

    float FrameTimeMs { get; }
    float MinFrameTimeMs { get; }
    float MaxFrameTimeMs { get; }

    long FrameIndex { get; }
    float UptimeSeconds { get; }

    /// <summary>Draw calls issued for the scene, excluding the overlay itself.</summary>
    int DrawCalls { get; }

    int SceneEntities { get; }
    string SceneName { get; }

    int CachedTextures { get; }
    int CachedMeshes { get; }

    string DeviceName { get; }
    (int Width, int Height) Resolution { get; }
}
