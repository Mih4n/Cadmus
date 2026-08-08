using System.Numerics;
using Cadmus.Core.Components;
using Cadmus.Engine.Geometry;

namespace Cadmus.Engine.Components.Sprites;

/// <summary>
/// A textured quad. <see cref="Size"/> is in pixels because the unit quad spans 1x1 and the default
/// camera is pixel-space orthographic. An optional child <see cref="PositionComponent"/> offsets the
/// sprite from its owning entity.
/// </summary>
public class SpriteComponent : ComposeComponent
{
    public Mesh Mesh { get; set; }
    public string TexturePath { get; set; }
    public float Rotation { get; set; }
    public Vector2 Size { get; set; }
    public Vector4 Tint { get; set; } = Vector4.One;
    public bool IsVisible { get; set; } = true;

    public SpriteComponent(
        Mesh mesh,
        string texturePath,
        float rotation = 0f,
        Vector2? size = null,
        params IEnumerable<IComponent> components)
    {
        Mesh = mesh;
        TexturePath = texturePath;
        Rotation = rotation;
        Size = size ?? Vector2.One;
        AddComponents(components);
    }

    // Mesh.UnitQuad, not a fresh CreateUnitQuad(): the GPU resource cache keys meshes by reference,
    // so a new instance per sprite would upload a new vertex buffer every time.
    public SpriteComponent(string texturePath, params IEnumerable<IComponent> components)
        : this(Mesh.UnitQuad, texturePath, components: components) { }

    public SpriteComponent(string texturePath, Vector2 size, params IEnumerable<IComponent> components)
        : this(Mesh.UnitQuad, texturePath, size: size, components: components) { }

    /// <summary>Local offset relative to the owning entity, if one was attached.</summary>
    public Vector3 LocalPosition => GetComponent<PositionComponent>()?.Vector ?? Vector3.Zero;

    public Matrix4x4 ComputeModelMatrix(Vector3 parentPosition)
    {
        var position = parentPosition + LocalPosition;

        return Matrix4x4.CreateScale(new Vector3(Size, 1f))
             * Matrix4x4.CreateRotationZ(Rotation)
             * Matrix4x4.CreateTranslation(position);
    }
}
