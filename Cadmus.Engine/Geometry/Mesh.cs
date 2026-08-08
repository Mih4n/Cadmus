using System.Numerics;

namespace Cadmus.Engine.Geometry;

public class Mesh
{
    public ushort[] Indices { get; }
    public Vector2[] UVs { get; }
    public Vector3[] Positions { get; }

    public Mesh(Vector3[] positions, Vector2[] uvs, ushort[] indices)
    {
        UVs = uvs;
        Indices = indices;
        Positions = positions;
    }

    /// <summary>
    /// A unit quad sampling only part of its texture — the basis for sprite sheets and font atlases.
    /// UV coordinates run V-downwards, like the pixel-space camera.
    /// </summary>
    public static Mesh CreateQuad(Vector2 uvMin, Vector2 uvMax)
    {
        var positions = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
        };
        var uvs = new[]
        {
            new Vector2(uvMin.X, uvMin.Y),
            new Vector2(uvMax.X, uvMin.Y),
            new Vector2(uvMax.X, uvMax.Y),
            new Vector2(uvMin.X, uvMax.Y),
        };
        var indices = new ushort[] { 0, 1, 2, 0, 2, 3 };

        return new Mesh(positions, uvs, indices);
    }

    /// <summary>
    /// The shared unit quad. Prefer this over <see cref="CreateUnitQuad"/> for throwaway sprites:
    /// the GPU cache keys meshes by reference, so reusing one instance uploads it once.
    /// </summary>
    public static Mesh UnitQuad { get; } = CreateUnitQuad();

    public static Mesh CreateUnitQuad()
    {
        var positions = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
        };
        // V grows downwards to match both the image layout and the Y-down pixel camera, so the
        // top-left vertex samples the top-left texel.
        var uvs = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
        };
        var indices = new ushort[] { 0, 1, 2, 0, 2, 3 };
        return new Mesh(positions, uvs, indices);
    }
}