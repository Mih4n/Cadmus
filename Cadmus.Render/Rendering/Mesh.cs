using System.Numerics;

namespace Cadmus.Render.Rendering;

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

    public static Mesh CreateUnitQuad()
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
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
        };
        var indices = new ushort[] { 0, 1, 2, 0, 2, 3 };
        return new Mesh(positions, uvs, indices);
    }
}