using Veldrid;

namespace Cadmus.Render.Rendering;

public interface IRenderBackend : IDisposable
{
    /// <summary>Initialize backend (create pipelines, layouts). Called after GraphicsDevice is available.</summary>
    void Initialize(GraphicsDevice gd, CommandList cl, ResourceFactory factory);

    /// <summary>Draw a single sprite (mesh+material+model matrix). Implementations should bind resources and issue draw calls.</summary>
    void DrawSprite(Sprite sprite);

    /// <summary>Call at frame begin if backend needs it.</summary>
    void BeginFrame();

    /// <summary>Call at frame end if backend needs it.</summary>
    void EndFrame();
}