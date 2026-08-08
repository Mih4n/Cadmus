using Cadmus.Core.Components;
using Cadmus.Engine.Geometry;

namespace Cadmus.Engine.Components;

public class MeshComponent : IComponent
{
    public Mesh Mesh { get; }

    public MeshComponent(Mesh mesh)
    {
        Mesh = mesh;
    }
}
