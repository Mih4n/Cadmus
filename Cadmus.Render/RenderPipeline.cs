using Cadmus.Render.Camera;
using Cadmus.Render.Rendering;

namespace Cadmus.Render;

public class RenderPipeline
{
    public Camera2D Camera { get; }
    
    private IRenderBackend backend;
    private readonly List<Sprite> submissions = new();

    public RenderPipeline(Camera2D camera)
    {
        Camera = camera ?? throw new ArgumentNullException(nameof(camera));
    }

    public void AttachBackend(IRenderBackend backend)
    {
        this.backend = backend;
    }

    public void BeginFrame()
    {
        submissions.Clear();
        backend?.BeginFrame();
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
            var shader = sprite.Material.Shader;

            shader.Model = model;
            shader.Projection = viewProjection;
            shader.BindTextures(sprite.Material.Textures);

            if (backend != null)
            {
                backend.DrawSprite(sprite);
            }
            else
            {
                SimulateDraw(sprite);
            }
        }

        backend?.EndFrame();
    }

    private void SimulateDraw(Sprite sprite)
    {
        Console.WriteLine($"[Render] Draw Sprite at ({sprite.Position.X:F1},{sprite.Position.Y:F1},{sprite.Position.Z:F2}) " +
                            $"scale({sprite.Scale.X:F2},{sprite.Scale.Y:F2}) shader={sprite.Material.Shader.Name}");
    }
}