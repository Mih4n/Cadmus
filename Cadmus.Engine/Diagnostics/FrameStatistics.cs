using Cadmus.Core.Diagnostics;
using Cadmus.Core.Game;
using Cadmus.Core.Scenes;
using Cadmus.Core.Systems;

namespace Cadmus.Engine.Diagnostics;

/// <summary>
/// Collects the per-frame numbers. It is a system so timing is sampled by the host itself; the
/// render layer fills in the fields only it can know (draw calls, GPU caches, resolution).
/// </summary>
public sealed class FrameStatistics(ISceneManager scenes) : IFrameStatistics, ISystem
{
    private const int SampleCount = 120;

    private readonly float[] samples = new float[SampleCount];
    private int sampleIndex;
    private int sampleFilled;

    /// <summary>Right after input, long before rendering.</summary>
    public int Order => int.MinValue + 1;

    public float Fps { get; private set; }
    public float FrameTimeMs { get; private set; }
    public float MinFrameTimeMs { get; private set; }
    public float MaxFrameTimeMs { get; private set; }
    public long FrameIndex { get; private set; }
    public float UptimeSeconds { get; private set; }

    public int DrawCalls { get; set; }
    public int SceneEntities { get; private set; }
    public string SceneName { get; private set; } = "none";
    public int CachedTextures { get; set; }
    public int CachedMeshes { get; set; }
    public string DeviceName { get; set; } = "unknown";
    public (int Width, int Height) Resolution { get; set; }

    public ValueTask UpdateAsync(GameTime time, CancellationToken cancellationToken = default)
    {
        FrameIndex = time.FrameIndex;
        UptimeSeconds = time.TotalSeconds;
        FrameTimeMs = time.DeltaSeconds * 1000f;

        // The very first frame carries the whole startup cost; it would skew the window for a while.
        if (time.FrameIndex > 0)
        {
            samples[sampleIndex] = FrameTimeMs;
            sampleIndex = (sampleIndex + 1) % SampleCount;
            sampleFilled = Math.Min(sampleFilled + 1, SampleCount);

            Recalculate();
        }

        var scene = scenes.Current;
        SceneEntities = scene?.Entities.Count ?? 0;
        SceneName = scene?.Name ?? "none";

        return ValueTask.CompletedTask;
    }

    private void Recalculate()
    {
        var total = 0f;
        var min = float.MaxValue;
        var max = 0f;

        for (int i = 0; i < sampleFilled; i++)
        {
            var sample = samples[i];
            total += sample;
            min = MathF.Min(min, sample);
            max = MathF.Max(max, sample);
        }

        var average = total / sampleFilled;

        MinFrameTimeMs = min;
        MaxFrameTimeMs = max;
        Fps = average > 0f ? 1000f / average : 0f;
    }
}
