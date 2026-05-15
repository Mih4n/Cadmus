using System.Numerics;
using Cadmus.Domain.Contracts.Components;

namespace Cadmus.Domain.Components;

public class CameraComponent : IComponent
{
    public float FieldOfView { get; set; }
    public float NearPlane { get; set; }
    public float FarPlane { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Target { get; set; }
    public Vector3 Up { get; set; }

    public CameraComponent()
    {
        FieldOfView = 45f;
        NearPlane = 0.1f;
        FarPlane = 100f;
        Position = new Vector3(0, 0, 3);
        Target = Vector3.Zero;
        Up = Vector3.UnitY;
    }

    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(Position, Target, Up);
    }

    public Matrix4x4 GetProjectionMatrix(float aspectRatio)
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(
            float.DegreesToRadians(FieldOfView),
            aspectRatio,
            NearPlane,
            FarPlane);
    }
}
