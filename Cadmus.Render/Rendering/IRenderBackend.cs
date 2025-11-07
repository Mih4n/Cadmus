using System.Numerics;

namespace Cadmus.Render.Rendering;

public interface IRenderBackend : IDisposable
{
    /// <summary>Draw a single sprite (mesh+material+model matrix). Implementations should bind resources and issue draw calls.</summary>
    void DrawSprite(Sprite sprite, Matrix4x4 view);

    /// <summary>Call at frame begin if backend needs it.</summary>
    void BeginFrame();

    /// <summary>Call at frame end if backend needs it.</summary>
    void EndFrame();
}