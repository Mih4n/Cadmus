using System.Numerics;
using Cadmus.Domain.Components;
using Cadmus.Domain.Components.Sprites;
using Cadmus.Render.Camera;
using Cadmus.Render.Rendering;

namespace Cadmus.Render;

public class RenderPipeline
{
    public Camera2D Camera { get; }
    
    private IRenderBackend backend;
    private readonly List<SpriteComponent> submissions = new();

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

    public void SubmitSprite(SpriteComponent sprite)
    {
        if (sprite is null) throw new ArgumentNullException(nameof(sprite));
        submissions.Add(sprite);
    }

    public void EndFrame()
    {
        var sorted = submissions
            .OrderBy(s => s.TryGetComponent<PositionComponent>(out var component) ? component.X : 0)
            .ToArray();
        var viewProjection = Camera.GetViewProjection();

        foreach (var sprite in sorted)
        {
            var model = sprite.ComputeModelMatrix();
            backend.DrawSprite(sprite, viewProjection);
        }

        backend?.EndFrame();
    }
}