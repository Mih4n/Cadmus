using Cadmus.Render.Camera;
using Cadmus.Render.Rendering;

namespace Cadmus.Render;

public class RenderPipeline
{
    public Camera2D Camera { get; }
    
    private IRenderBackend backend;
    private readonly List<Sprite> submissions = new();

    public RenderPipeline(Camera2D camera, IRenderBackend backend)
    {
        Camera = camera; 
        this.backend = backend;
    }

    public void BeginFrame()
    {
        submissions.Clear();
        backend.BeginFrame();
    }

    public void SubmitSprite(Sprite sprite)
    {
        if (sprite is null) throw new ArgumentNullException(nameof(sprite));
        submissions.Add(sprite);
    }

    public void EndFrame()
    {
        var sorted = submissions.OrderBy(s => s.Position.Z).ToArray();
        var viewProjection = Camera.GetViewProjection();

        foreach (var sprite in sorted)
        {
            var model = sprite.ComputeModelMatrix();
            backend.DrawSprite(sprite, viewProjection);
        }

        backend?.EndFrame();
    }
}