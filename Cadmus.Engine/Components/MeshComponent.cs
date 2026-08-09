using Cadmus.Engine.Geometry;

namespace Cadmus.Engine.Components;

public class MeshComponent : Component
{
    public Mesh Mesh { get; }

    public MeshComponent(Mesh mesh)
    {
        Mesh = mesh;
    }
}
