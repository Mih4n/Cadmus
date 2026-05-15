using Cadmus.Domain.Contracts.Components;
using Cadmus.Domain.Rendering;

namespace Cadmus.Domain.Components;

public class MeshComponent : IComponent
{
    public Mesh Mesh { get; }

    public MeshComponent(Mesh mesh)
    {
        Mesh = mesh;
    }
}
