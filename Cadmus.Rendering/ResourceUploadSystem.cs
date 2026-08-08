using Cadmus.Core.Game;
using Cadmus.Core.Scenes;
using Cadmus.Core.Systems;
using Cadmus.Graphics.Resources;

namespace Cadmus.Rendering;

/// <summary>
/// Warms the GPU cache for everything the current scene will draw. Uploads block on a queue wait,
/// so doing them here — before the frame's command buffer is recorded — keeps that stall out of the
/// render path.
/// </summary>
public sealed class ResourceUploadSystem(
    ISceneManager scenes,
    IGpuResourceCache resources,
    RenderItemCollector collector) : ISystem
{
    /// <summary>Runs before the renderer, which sits at <see cref="int.MaxValue"/>.</summary>
    public int Order => 1000;

    public ValueTask UpdateAsync(GameTime time, CancellationToken cancellationToken = default)
    {
        foreach (var item in collector.Collect(scenes.Current))
        {
            resources.GetMesh(item.Mesh);
            resources.GetTextureDescriptor(item.TexturePath);
        }

        return ValueTask.CompletedTask;
    }
}
