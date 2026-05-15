using System.Numerics;
using Cadmus.Domain.Contracts.Components;

namespace Cadmus.Domain.Components;

public class TransformComponent : IComponent
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public Vector3 Scale { get; set; }

    public TransformComponent()
    {
        Position = Vector3.Zero;
        Rotation = Quaternion.Identity;
        Scale = Vector3.One;
    }

    public TransformComponent(Vector3 position, Quaternion? rotation = null, Vector3? scale = null)
    {
        Position = position;
        Rotation = rotation ?? Quaternion.Identity;
        Scale = scale ?? Vector3.One;
    }

    public Matrix4x4 GetModelMatrix()
    {
        var scaleMat = Matrix4x4.CreateScale(Scale);
        var rotationMat = Matrix4x4.CreateFromQuaternion(Rotation);
        var translationMat = Matrix4x4.CreateTranslation(Position);
        return scaleMat * rotationMat * translationMat;
    }
}
