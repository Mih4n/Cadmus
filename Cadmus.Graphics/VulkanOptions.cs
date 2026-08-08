using System.Numerics;

namespace Cadmus.Graphics;

/// <summary>
/// Backend configuration. Registered as a singleton, injected wherever it is needed — no statics.
/// Relative asset paths are resolved against <see cref="AppContext.BaseDirectory"/>.
/// </summary>
public sealed class VulkanOptions
{
    public string ApplicationName { get; set; } = "Cadmus";

    /// <summary>Enables the Khronos validation layer when it is installed. Defaults to DEBUG builds.</summary>
    public bool EnableValidation { get; set; } =
#if DEBUG
        true;
#else
        false;
#endif

    /// <summary>Frames the CPU may run ahead of the GPU.</summary>
    public uint FramesInFlight { get; set; } = 2;

    public string VertexShaderPath { get; set; } = "Assets/Shaders/sprite.vert.spv";
    public string FragmentShaderPath { get; set; } = "Assets/Shaders/sprite.frag.spv";
    public string FallbackTexturePath { get; set; } = "Assets/Textures/fallback.png";

    /// <summary>Flat 1-colour texture; tint it to draw solid shapes.</summary>
    public string WhiteTexturePath { get; set; } = "Assets/Textures/white.png";

    /// <summary>ASCII atlas used by the debug overlay; 16x24 cells, 16 per row.</summary>
    public string FontTexturePath { get; set; } = "Assets/Textures/font.png";

    /// <summary>Linear space — the swapchain is sRGB and converts on write.</summary>
    public Vector4 ClearColor { get; set; } = new(0.011f, 0.013f, 0.018f, 1f);

    /// <summary>Upper bound of draw calls per frame; sizes the per-object uniform ring.</summary>
    public int MaxDrawsPerFrame { get; set; } = 4096;

    /// <summary>Upper bound of simultaneously bound textures; sizes the descriptor pool.</summary>
    public int MaxTextures { get; set; } = 512;

    public static string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
}
