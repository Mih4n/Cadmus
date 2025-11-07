using System.Numerics;

namespace Cadmus.Render.Camera;

public class Camera2D
{
    public int ViewportWidth { get; set; } = 800;
    public int ViewportHeight { get; set; } = 600;

    public float Zoom { get; set; } = 1f;
    public float RotationRadians { get; set; } = 0f;

    public Vector2 Position { get; set; } = Vector2.Zero;

    public Camera2D() { }

    public Matrix4x4 GetViewMatrix()
    {
        var scale = Matrix4x4.CreateScale(Zoom, Zoom, 1f);
        var rotate = Matrix4x4.CreateRotationZ(-RotationRadians);
        var translate = Matrix4x4.CreateTranslation(new Vector3(-Position, 0f));

        return translate * rotate * scale;
    }

    public Matrix4x4 GetProjectionMatrix()
    {
        float halfW = ViewportWidth * 0.5f;
        float halfH = ViewportHeight * 0.5f;

        float top = -halfH;
        float left = -halfW;
        float right = halfW;
        float bottom = halfH;

        var ortho = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, -1000f, 1000f);
        return ortho;
    }

    public Matrix4x4 GetViewProjection()
    {
        return GetViewMatrix() * GetProjectionMatrix();
    }
}