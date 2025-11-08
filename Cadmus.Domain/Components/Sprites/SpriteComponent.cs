using System.Numerics;
using Cadmus.Domain.Contracts.Components;
using Cadmus.Render.Rendering;

namespace Cadmus.Domain.Components.Sprites;

public class SpriteComponent : ComposeComponent
{
    public Mesh Mesh { get; set; }
    public string Path { get; set; }

    public bool Loaded { get; set; } = false;
    public float Rotation { get; set; }
    public Vector2 Scale { get; set; }

    private static PositionComponent relativePositionBase = Vector3.Zero;

    public SpriteComponent(
        Mesh mesh, 
        string path,
        float rotation = 0f,
        Vector2? scale = null,
        params IEnumerable<IComponent> components
    )
    {
        Mesh = mesh;
        Path = path;
        Rotation = rotation;
        Scale = scale ?? Vector2.One;
        AddComponents(components);
    }

    public SpriteComponent(string path, params IEnumerable<IComponent> components) 
        : this(Mesh.CreateUnitQuad(), path, components: components) {}

    public Matrix4x4 ComputeModelMatrix(Vector3? parentPosition = null)
    {
        var relativePosition = GetComponent<PositionComponent>() ?? relativePositionBase;
        var position = relativePosition + (parentPosition ?? Vector3.Zero);

        var scaleMat = Matrix4x4.CreateScale(new Vector3(Scale, 1f));
        var rotationMat = Matrix4x4.CreateRotationZ(Rotation);
        var translationMat = Matrix4x4.CreateTranslation(position);

        return scaleMat * rotationMat * translationMat;
    }
}
