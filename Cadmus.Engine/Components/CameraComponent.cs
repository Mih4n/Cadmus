using System.Numerics;
using Cadmus.Core.Components;

namespace Cadmus.Engine.Components;

public enum ProjectionMode
{
    /// <summary>Pixel-space projection: (0,0) is the top-left corner, Y grows downwards.</summary>
    Orthographic,
    Perspective
}

public class CameraComponent : IComponent
{
    public ProjectionMode Mode { get; set; } = ProjectionMode.Orthographic;

    public float FieldOfView { get; set; } = 45f;
    public float NearPlane { get; set; } = -1000f;
    public float FarPlane { get; set; } = 1000f;

    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Target { get; set; } = -Vector3.UnitZ;
    public Vector3 Up { get; set; } = Vector3.UnitY;

    /// <summary>Orthographic magnification: 2 renders everything at double size.</summary>
    public float Zoom { get; set; } = 1f;

    public Matrix4x4 GetViewMatrix() => Mode switch
    {
        ProjectionMode.Perspective => Matrix4x4.CreateLookAt(Position, Target, Up),
        _ => Matrix4x4.CreateTranslation(-Position) * Matrix4x4.CreateScale(Zoom, Zoom, 1f)
    };

    public Matrix4x4 GetProjectionMatrix(int width, int height)
    {
        if (Mode == ProjectionMode.Perspective)
        {
            var aspect = height == 0 ? 1f : (float)width / height;
            return Matrix4x4.CreatePerspectiveFieldOfView(
                float.DegreesToRadians(FieldOfView),
                aspect,
                MathF.Max(NearPlane, 0.01f),
                MathF.Max(FarPlane, 1f));
        }

        // bottom = 0, top = height puts the origin at the top-left under Vulkan's Y-down NDC.
        return Matrix4x4.CreateOrthographicOffCenter(0, width, 0, height, NearPlane, FarPlane);
    }

    /// <summary>
    /// Combined matrix in System.Numerics (row-vector) order. Uploading it verbatim is correct for
    /// GLSL's <c>u_ViewProj * u_Model * v</c> because the raw bytes are read back transposed.
    /// </summary>
    public Matrix4x4 GetViewProjection(int width, int height) =>
        GetViewMatrix() * GetProjectionMatrix(width, height);
}
