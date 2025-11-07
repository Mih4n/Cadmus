using System.Numerics;

namespace Cadmus.Render.Rendering;

public class Sprite
{
    public Mesh Mesh { get; }
    public Material Material { get; }

    public float RotationRadians { get; set; } = 0f; 
    public Vector2 Scale { get; set; } = Vector2.One;
    public Vector3 Position { get; set; } = Vector3.Zero;

    public Sprite(Mesh mesh, Material material)
    {
        Mesh = mesh;
        Material = material;
    }

    public Matrix4x4 ComputeModelMatrix()
    {
        var scale = Matrix4x4.CreateScale(new Vector3(Scale, 1f));
        var rotation = Matrix4x4.CreateRotationZ(RotationRadians);
        var translation = Matrix4x4.CreateTranslation(Position);

        return scale * rotation * translation;
    }
}